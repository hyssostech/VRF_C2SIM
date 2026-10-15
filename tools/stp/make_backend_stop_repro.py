"""Build the SANITIZED back-end-stop repro (D47 follow-up): a synthetic C2SIM Initialization with ONE generic infantry
brigade container and a 2-task Order of the shape that stopped the VR-Forces 5.2d back end 4 of 4 times.

Usage: make_backend_stop_repro.py <out_dir> <dest_lat> <dest_lon> [tag]
Writes <out_dir>/repro_init.xml and <out_dir>/repro_order_<tag>.xml (ASCII, LF).

NO CUSTOMER DATA: generic names (SIDE_A, SIDE_B, TEST_BDE, TEST_MOVE_1/2), fixed generic uuids, no task organisation, no
plan. The unit is an infantry brigade (SFGPUCI----H---, echelon BDE) - the interface populates it from its own authored
composition table as an aggregate container with 17 members (generic member names derived from TEST_BDE). Its start is
the position the stops happened from, ROUNDED to 4 decimals (~10 m): 54.0194 N, 23.3139 E.
Order shape (the same as every stop): task 1 = a move (CONSTR, "construct - not simulated", which the interface drives
as a plain move) to the destination; task 2 = a second move to the SAME destination, released STREND after task 1
(ENGAGE: an attack-family verb, advanced as a move with fire at will). Both ROEHold, 60-min slots.
"""
import sys

NS = 'http://www.sisostds.org/schemas/C2SIM/1.1'
SIDE_A = '00000000-0000-00aa-0000-000000000001'
SIDE_B = '00000000-0000-00aa-0000-000000000002'
UNIT = '00000000-0000-00aa-0001-000000000001'
START = (54.0194, 23.3139)


def init_xml():
    return f'''<?xml version="1.0" encoding="utf-8"?>
<MessageBody xmlns="{NS}">
  <C2SIMInitializationBody>
    <ObjectDefinitions>
      <AbstractObject>
        <ForceSide>
          <Name>SIDE_A</Name>
          <UUID>{SIDE_A}</UUID>
          <ForceSideRelation>
            <HostilityStatusCode>HO</HostilityStatusCode>
            <OtherSide>{SIDE_B}</OtherSide>
          </ForceSideRelation>
        </ForceSide>
      </AbstractObject>
      <AbstractObject>
        <ForceSide>
          <Name>SIDE_B</Name>
          <UUID>{SIDE_B}</UUID>
          <ForceSideRelation>
            <HostilityStatusCode>HO</HostilityStatusCode>
            <OtherSide>{SIDE_A}</OtherSide>
          </ForceSideRelation>
        </ForceSide>
      </AbstractObject>
      <Entity>
        <ActorEntity>
          <CollectiveEntity>
            <MilitaryOrganization>
              <Unit>
                <EntityDescriptor>
                  <Side>{SIDE_A}</Side>
                </EntityDescriptor>
                <CurrentState>
                  <PhysicalState>
                    <EntityHealthStatus>
                      <OperationalStatus>
                        <OperationalStatusCode>FullyOperational</OperationalStatusCode>
                      </OperationalStatus>
                    </EntityHealthStatus>
                    <Location>
                      <GeodeticCoordinate>
                        <Latitude>{START[0]}</Latitude>
                        <Longitude>{START[1]}</Longitude>
                      </GeodeticCoordinate>
                    </Location>
                  </PhysicalState>
                </CurrentState>
                <APP6CSymbol>
                  <APP6C-SIDC>SFGPUCI----H---</APP6C-SIDC>
                </APP6CSymbol>
                <Name>TEST_BDE</Name>
                <SISOEntityType>
                  <DISCategory>0</DISCategory>
                  <DISCountry>0</DISCountry>
                  <DISDomain>0</DISDomain>
                  <DISExtra>0</DISExtra>
                  <DISKind>0</DISKind>
                  <DISSpecific>0</DISSpecific>
                  <DISSubCategory>0</DISSubCategory>
                </SISOEntityType>
                <UUID>{UNIT}</UUID>
                <EchelonCode>BDE</EchelonCode>
              </Unit>
            </MilitaryOrganization>
          </CollectiveEntity>
        </ActorEntity>
      </Entity>
    </ObjectDefinitions>
    <ScenarioSetting>
      <DateTime>
        <IsoDateTime>2026-10-15T06:00:00Z</IsoDateTime>
      </DateTime>
      <Version>1.0.2</Version>
    </ScenarioSetting>
    <SystemEntityList>
      <ActorReference>{UNIT}</ActorReference>
      <SystemName>STP</SystemName>
    </SystemEntityList>
  </C2SIMInitializationBody>
</MessageBody>
'''


def task(name, tid, code, lat, lon, start_s, after=None):
    rel = ''
    if after:
        rel = ('          <ActionTemporalRelationship><ActionTemporalAssociationCode>STREND</ActionTemporalAssociationCode>'
               f'<TemporalAssociationWithAction>{after}</TemporalAssociationWithAction></ActionTemporalRelationship>\n')
    h, m = divmod(start_s // 60, 60)
    return (f'''      <Task>
        <ManeuverWarfareTask>
{rel}          <Location><GeodeticCoordinate><Latitude>{lat}</Latitude><Longitude>{lon}</Longitude></GeodeticCoordinate></Location>
          <Name>{name}</Name>
          <UUID>{tid}</UUID>
          <AffectedEntity>{UNIT}</AffectedEntity>
          <DesiredEffectCode>TaskSuccess</DesiredEffectCode>
          <Duration><IsoTimeDuration>P00Y00M00DT01H00M00S</IsoTimeDuration></Duration>
          <PerformingEntity>{UNIT}</PerformingEntity>
          <StartTime><SimulationTime><DelayTimeAmount><IsoTimeDuration>P00Y00M00DT{h:02d}H{m:02d}M00S</IsoTimeDuration></DelayTimeAmount></SimulationTime></StartTime>
          <TaskActionCode>{code}</TaskActionCode>
          <RuleOfEngagement><MipWeaponUseROE><WeaponROECode><WeaponRuleOfEngagementCode>ROEHold</WeaponRuleOfEngagementCode></WeaponROECode></MipWeaponUseROE></RuleOfEngagement>
        </ManeuverWarfareTask>
      </Task>
''')


def order_xml(lat, lon, tag):
    t1 = f'00000000-0000-00aa-0002-0000000000{tag}1'
    t2 = f'00000000-0000-00aa-0002-0000000000{tag}2'
    return (f'''<?xml version="1.0" encoding="utf-8"?>
<MessageBody xmlns="{NS}">
  <DomainMessageBody>
    <OrderBody>
      <FromSender>00000000-0000-0001-0001-000000000000</FromSender>
      <ToReceiver>00000000-0007-0001-0000-000000000000</ToReceiver>
      <IssuedTime><IsoDateTime>2026-10-15T06:00:00Z</IsoDateTime></IssuedTime>
      <OrderID>00000000-0000-00aa-0003-0000000000{tag}0</OrderID>
''' + task('TEST_MOVE_1', t1, 'CONSTR', lat, lon, 0)
            + task('TEST_MOVE_2', t2, 'ENGAGE', lat, lon, 3600, after=t1) + '''    </OrderBody>
  </DomainMessageBody>
</MessageBody>
''')


def main(argv):
    if len(argv) < 4:
        print(__doc__)
        return 2
    out, lat, lon = argv[1], float(argv[2]), float(argv[3])
    tag = (argv[4] if len(argv) > 4 else 'a')[:1].lower()
    if tag not in '0123456789abcdef':
        print('tag must be one hex digit')
        return 2
    with open(f'{out}/repro_init.xml', 'w', encoding='utf-8', newline='\n') as f:
        f.write(init_xml())
    with open(f'{out}/repro_order_{tag}.xml', 'w', encoding='utf-8', newline='\n') as f:
        f.write(order_xml(lat, lon, tag))
    print(f'repro_init.xml (TEST_BDE at {START}); repro_order_{tag}.xml: TEST_MOVE_1 CONSTR -> TEST_MOVE_2 ENGAGE (STREND), '
          f'both to ({lat},{lon})')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
