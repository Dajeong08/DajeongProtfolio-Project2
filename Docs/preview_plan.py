from pathlib import Path
import pypdfium2 as pdfium
from PIL import Image,ImageOps,ImageDraw
base=Path(__file__).parent/'plan_preview'
pdf=pdfium.PdfDocument(str(base/'plan.pdf'))
thumbs=[]
for i,page in enumerate(pdf):
 text=page.get_textpage().get_text_range()
 print(i+1, len(text), text[:70].replace('\r',' ').replace('\n',' '))
 im=page.render(scale=1.6).to_pil();im.save(base/f'page-{i+1}.png')
 im.thumbnail((360,510));thumb=Image.new('RGB',(380,540),'#dddddd');thumb.paste(im,(10,20));ImageDraw.Draw(thumb).text((10,3),str(i+1),fill='black');thumbs.append(thumb)
canvas=Image.new('RGB',(380*3,540*((len(thumbs)+2)//3)),'white')
for i,im in enumerate(thumbs):canvas.paste(im,((i%3)*380,(i//3)*540))
canvas.save(base/'overview.png')
