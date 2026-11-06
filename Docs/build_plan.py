import json
from pathlib import Path
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT
base=Path(__file__).parent
pages=json.loads((base/'plan_content.json').read_text(encoding='utf-8'))
doc=Document()
sec=doc.sections[0]
sec.page_width=Cm(21);sec.page_height=Cm(29.7)
sec.top_margin=Cm(1.7);sec.bottom_margin=Cm(1.7);sec.left_margin=Cm(1.9);sec.right_margin=Cm(1.9)
for name,size in [('Normal',10.5),('Title',23),('Heading 1',18),('Heading 2',12)]:
 st=doc.styles[name];st.font.name='Malgun Gothic';st.font.size=Pt(size);st.font.color.rgb=RGBColor(0,0,0)
 st.element.get_or_add_rPr().rFonts.set(qn('w:eastAsia'),'Malgun Gothic')
 st.paragraph_format.space_after=Pt(4)
 st.paragraph_format.space_before=Pt(0)
 snap=OxmlElement('w:snapToGrid');snap.set(qn('w:val'),'0');st.element.get_or_add_pPr().append(snap)
 st.paragraph_format.line_spacing=Pt(size * 1.35)
 if name in ['Title','Heading 1','Heading 2']:st.paragraph_format.keep_with_next=True
normal=doc.styles['Normal'];normal.paragraph_format.widow_control=True
for pi,page in enumerate(pages):
 if pi:doc.add_page_break()
 doc.add_paragraph(page['title'],'Title' if pi==0 else 'Heading 1')
 if pi==0:doc.add_paragraph('기능 설계와 개발 순서  |  2026년 11월 6일')
 if page.get('intro'):doc.add_paragraph(page['intro'])
 for section in page['sections']:
  doc.add_paragraph(section['h'],'Heading 2')
  for text in section.get('p',[]):doc.add_paragraph(text)
  if 'table' in section:
   data=section['table'];t=doc.add_table(rows=0,cols=len(data[0]));t.autofit=False
   widths=[3.8,13.4] if len(data[0])==2 else [1.4,7.0,8.8]
   for col,width in zip(t.columns,widths):col.width=Cm(width)
   borders=OxmlElement('w:tblBorders')
   for edge in ['top','left','bottom','right','insideH','insideV']:
    el=OxmlElement('w:'+edge);el.set(qn('w:val'),'single');el.set(qn('w:sz'),'4');el.set(qn('w:color'),'D9D9D9');borders.append(el)
   t._tbl.tblPr.append(borders)
   for ri,row in enumerate(data):
    cells=t.add_row().cells
    trPr=t.rows[-1]._tr.get_or_add_trPr();trPr.append(OxmlElement('w:cantSplit'))
    if ri==0:trPr.append(OxmlElement('w:tblHeader'))
    for ci,(cell,text) in enumerate(zip(cells,row)):
     cell.width=Cm(widths[ci]);cell.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
     cell.text=text
     pr=cell._tc.get_or_add_tcPr();shade=OxmlElement('w:shd');shade.set(qn('w:fill'),'E5EDF4' if ri==0 else 'FFFFFF');pr.append(shade)
     margins=OxmlElement('w:tcMar')
     for side in ['top','left','bottom','right']:
      e=OxmlElement('w:'+side);e.set(qn('w:w'),'75');e.set(qn('w:type'),'dxa');margins.append(e)
     pr.append(margins)
     for p in cell.paragraphs:
      p.paragraph_format.space_after=Pt(0);p.paragraph_format.line_spacing=Pt(12.5)
      if len(row)==3 and ci==0:p.alignment=WD_ALIGN_PARAGRAPH.CENTER
      for r in p.runs:r.font.size=Pt(9.5);r.bold=(ri==0)
   spacer=doc.add_paragraph();spacer.paragraph_format.space_after=Pt(0);spacer.paragraph_format.space_before=Pt(0);spacer.paragraph_format.line_spacing=Pt(3);spacer.add_run().font.size=Pt(3)
footer=sec.footer.paragraphs[0];footer.alignment=WD_ALIGN_PARAGRAPH.RIGHT
run=footer.add_run();fld=OxmlElement('w:fldSimple');fld.set(qn('w:instr'),'PAGE');run._r.addnext(fld)
doc.core_properties.title='운빨 디펜스 게임 기획';doc.core_properties.subject='25웨이브 기반 기능 설계와 구현 순서'
doc.save(base/'Luck_Defense_Game_Plan.docx')
print(base/'Luck_Defense_Game_Plan.docx')
