"""Read the source Odin map binary, using the documented BinaryEntryType format.
Reference: https://github.com/TeamSirenix/odin-serializer (Apache-2.0).
Original assets are read-only; output is the narrow map data adapter.
"""
from pathlib import Path
import re,struct,json
class Reader:
    def __init__(self,data):self.data=data;self.at=0;self.types={};self.refs={}
    def number(self,fmt):
        size=struct.calcsize('<'+fmt);value=struct.unpack_from('<'+fmt,self.data,self.at)[0];self.at+=size;return value
    def string(self):
        encoding=self.number('B');length=self.number('i');size=length*(2 if encoding else 1)
        value=self.data[self.at:self.at+size].decode('utf-16-le' if encoding else 'latin-1');self.at+=size;return value
    def typ(self):
        tag=self.number('B')
        if tag==0x2e:return None
        ident=self.number('i')
        if tag==0x2f:self.types[ident]=self.string()
        elif tag!=0x30:raise ValueError(('type',tag,self.at))
        return self.types[ident]
    def entry(self):
        tag=self.number('B')
        named=tag in (1,3,9,11,13,50) or 15<=tag<=45 and tag%2==1
        name=self.string() if named else None
        if tag in (1,2,3,4):
            obj={'$type':self.typ()}
            if tag in (1,2):self.refs[self.number('i')]=obj
            while self.data[self.at]!=5:
                key,value=self.entry()
                if key is None:obj.setdefault('$values',[]).append(value)
                else:obj[key]=value
            self.at+=1;return name,obj
        if tag==6:
            size=self.number('q');values=[]
            while self.data[self.at]!=7:values.append(self.entry()[1])
            self.at+=1
            if size!=len(values):raise ValueError(('array size',size,len(values)))
            return name,values
        if tag==8:
            count=self.number('i');size=self.number('i');chunk=self.data[self.at:self.at+count*size];self.at+=count*size
            return name,list(chunk) if size==1 else list(struct.unpack('<'+('f' if size==4 else 'd')*count,chunk))
        if tag in (9,10):return name,self.refs[self.number('i')]
        if tag in (11,12):return name,{'external':self.number('i')}
        if tag in (13,14,41,42):self.at+=16;return name,None
        if tag in (45,46):return name,None
        if tag in (39,40,50,51):return name,self.string()
        formats={15:'b',16:'b',17:'B',18:'B',19:'h',20:'h',21:'H',22:'H',23:'i',24:'i',25:'I',26:'I',27:'q',28:'q',29:'Q',30:'Q',31:'f',32:'f',33:'d',34:'d',37:'H',38:'H',43:'B',44:'B'}
        if tag in formats:return name,self.number(formats[tag])
        raise ValueError(('entry',hex(tag),self.at))
def main():
    source=Path('D:/game/Bla_mobile_map/ExportedProject/Assets/Resources/new maps/CvstodiaDLC3.asset')
    text=source.read_text(encoding='utf-8-sig');data=bytes.fromhex(re.search(r'SerializedBytes: ([0-9a-f]+)',text)[1]);reader=Reader(data);root={}
    while reader.at<len(data):
        key,value=reader.entry();root[key]=value
    pairs=root['Cells']['CellsDict']['$values'][0];cells=[]
    for pair in pairs:
        key=pair['$k'];value=pair.get('$v')
        if not value:continue
        zone=value['ZoneId'];room=''.join(zone[k] for k in ['_district','_zone','_scene'])
        bounds=value['CalculatedWorldBounding']['$values']
        cells.append({'key':str(key['X'])+','+str(key['Y']),'gridX':key['X'],'gridY':key['Y'],'room':room,'type':value.get('Type',0),'ignored':bool(value.get('IgnoredForMapPercentage',0)),'ngPlus':bool(value.get('NGPlus',0)),
            'left':bounds[0],'bottom':bounds[1],'width':bounds[2],'height':bounds[3],'walls':value['Walls']['$values'][0],'doors':value['Doors']['$values'][0]})
    output={'cellWidth':20,'cellHeight':11,'cells':cells}
    Path('Assets/Brotherhood/Resources/Inventory/map-source.json').write_text(json.dumps(output,separators=(',',':')),encoding='utf-8')
    print('Source map cells',len(cells),'counted',sum(not c['ignored'] and not c['ngPlus'] for c in cells),'bytes consumed',reader.at)
    rooms=set(c['room'] for c in cells);print('Room IDs',len(rooms))
if __name__=='__main__':main()
