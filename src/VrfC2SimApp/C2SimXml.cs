using System.Xml;

namespace VrfC2SimApp;

/// <summary>
/// Root-element SNIFF for inbound C2SIM xml, so a parser calls the ONE ToC2SIMObject
/// overload that matches what it was handed instead of trying the wrong one first.
///
/// WHY (B5, 2026-09-14): InitParser/OrderParser used to call ToC2SIMObject&lt;MessageBodyType&gt;
/// speculatively and catch the failure. That call logs through the SDK's OWN logger BEFORE it
/// throws ("Failed to deserialize xml to type C2SIM.Schema102.MessageBodyType: There is an error
/// in XML document (1, 2)." - C2SIMSSDK.cs:823, inside ToC2SIMObject, so a caller's catch cannot
/// suppress it). That put two FALSE ERROR lines in every run log - one for the inbound init, one
/// for the inbound order - both for messages that then parsed perfectly on the second try.
///
/// The SDK's own STOMP pump does exactly this sniff (C2SIMSSDK.cs:639-676: read the body element's
/// LocalName, then switch on it), so the parsers now follow the vendor's own pattern.
///
/// XmlReader, not XElement.Parse: an inbound string may open with an xml declaration (every file
/// in data/) or with a comment (data/R9_Mojave_UnitMove_Order.xml), and the reader skips both to
/// the first ELEMENT without materializing the document.
/// </summary>
public static class C2SimXml
{
    /// <summary>Root of a pushed message FILE (and of the SDK's PushReportMessage envelope).</summary>
    public const string MessageBody = "MessageBody";

    /// <summary>The MessageBody child that carries Order/Report/Plan/Request bodies.</summary>
    public const string DomainMessageBody = "DomainMessageBody";

    /// <summary>
    /// The local name of the FIRST element in <paramref name="xml"/>, or "" when the string is
    /// empty or not well-formed enough to reach one. Never throws: an unreadable string simply
    /// has no root name, and the caller falls through to its own deserialize + catch.
    /// </summary>
    public static string RootLocalName(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return "";
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = true,
            IgnoreWhitespace = true,
            IgnoreProcessingInstructions = true,
            CloseInput = true,
        };
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read())
                if (reader.NodeType == XmlNodeType.Element) return reader.LocalName;
        }
        catch (XmlException) { /* not well-formed - the deserialize below will say so */ }
        return "";
    }
}
