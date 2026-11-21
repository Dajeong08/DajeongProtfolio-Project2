from pathlib import Path
import re,zlib,struct,json
root=Path.cwd();folder=root/'Assets/Animation/Character_Animation';libs=root/'Assets/SpriteLibrary/Characters'
def read(p):return p.read_text(encoding='utf-8-sig')
def guid(p):return re.search(r'^guid: (\w+)',read(Path(str(p)+'.meta')),re.M)[1]
def h(s):return zlib.crc32(s.encode())&0x3fffffff
def write(p,t):p.write_text(t,encoding='utf-8',newline='\r\n')
ctrls={guid(folder/(n+'.controller')):cat for n,cat in [('Slash','Slash'),('Jab','Jab'),('Punch','Jab'),('Bow','Shot')]}
ctrls[guid(root/'Assets/Animation/Knight.controller')]='Slash'
baseguid=guid(folder/'Slash.controller');clip=root/'Assets/Animation/Attack.anim';clipguid=guid(clip)
libpaths={guid(p):p for p in libs.iterdir() if p.suffix in ['.asset','.spriteLib']}
prepared={};report=[];expected={}
for prefab in (root/'Assets/Prefab/Character').glob('*.prefab'):
 t=read(prefab);old=re.search(r'm_Controller: \{fileID: 9100000, guid: (\w+)',t)
 if not old or old[1] not in ctrls:continue
 cat=ctrls[old[1]];lg=re.search(r'm_SpriteLibraryAsset: \{fileID: \d+, guid: (\w+)',t)[1];p=libpaths[lg];lt=read(p)
 assert lg not in expected or expected[lg]['source']==cat,'Shared library with different attacks'
 blocks=re.split(r'(?=^  - m_Name:)',lt,flags=re.M)
 source=next(b for b in blocks[1:] if re.match('  - m_Name: '+cat+r'\n',b))
 # The final category is followed by library metadata, which must not be copied into a category.
 source=re.split(r'(?=^  m_\w+:)',source,flags=re.M)[0]
 refs=re.findall(r'^      m_Sprite: (\{[^\n]+\})',source,re.M);assert len(refs) in [3,4],(p,len(refs))
 sprites=(refs+[refs[-1]])[:4]
 is_source=p.suffix=='.spriteLib'
 attack=f'  - m_Name: Attack\n    m_Hash: {h("Attack")}\n    m_CategoryList:'+(' []\n    m_OverrideEntries:\n' if is_source else '\n')
 for i,sprite in enumerate(sprites):
  attack+=f'    - m_Name: {i}\n      m_Hash: {h(str(i))}\n      m_Sprite: {sprite}\n'
  if is_source:attack+=f'      m_FromMain: 0\n      m_SpriteOverride: {sprite}\n'
 if is_source:attack+='    m_FromMain: 0\n    m_EntryOverrideCount: 4\n'
 assert '  - m_Name: Attack\n' not in lt
 # Insert before first category, preserving all existing entries and edits.
 first=lt.index('  - m_Name:');lt=lt[:first]+attack+lt[first:]
 prepared[p]=lt
 expected[lg]={'source':cat,'sprites':sprites,'library':str(p.relative_to(root))}
 t=t.replace(old[0],f'm_Controller: {{fileID: 9100000, guid: {baseguid}')
 prepared[prefab]=t;report.append({'character':prefab.stem,'source':cat,'library':p.name})
assert len(report)==19,len(report)
# One controller: retain its GUID so existing references to Slash also remain valid.
base=read(folder/'Slash.controller').replace('  m_Name: Slash\n','  m_Name: Attack\n')
base=re.sub(r'(m_Motion: \{fileID: 7400000, guid: )89f475f8146a48b8b589e3c11a0b3a63',lambda m:m[1]+clipguid,base)
assert base.count('  m_Name: Attack\n')==2
prepared[folder/'Attack.controller']=base
# One Attack clip: same track, binding, and four-frame timing; point at Attack_0..3 instead of Slash_0..3.
t=read(clip)
for i in range(4):
 oldhash=h('Slash_'+str(i));newhash=h('Attack_'+str(i));count=[0]
 def replace(m):
  bits=struct.unpack('<I',struct.pack('<f',float(m[1])))[0]
  if bits!=oldhash:return m[0]
  count[0]+=1;return 'value: '+format(struct.unpack('<f',struct.pack('<I',newhash))[0],'.9g')
 t=re.sub(r'value: ([^\s]+)',replace,t)
 assert count[0]==(4 if i==0 else 2),(i,count)
prepared[clip]=t
# Record original frame mappings for static verification.
(root/'tmp/animation-unify/mappings.json').write_text(json.dumps(expected,indent=2),encoding='utf-8')
for p,t in prepared.items():write(p,t)
(folder/'Slash.controller.meta').rename(folder/'Attack.controller.meta')
(folder/'Slash.controller').unlink()
print(json.dumps(report,indent=2));print('Unified 19 character prefabs and libraries, Attack controller and Attack clip.')

