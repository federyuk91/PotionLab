from pathlib import Path
import re
from xml.sax.saxutils import escape
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4
from reportlab.lib.colors import HexColor
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import Paragraph
from reportlab.lib.styles import ParagraphStyle
from pypdf import PdfReader

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'output/pdf/The_Goodnight_Potion_Voice_Actor_Script_EN.pdf'
OUT.parent.mkdir(parents=True, exist_ok=True)
for name, filename in [('Body', 'calibri.ttf'), ('Bold', 'calibrib.ttf'), ('Italic', 'calibrii.ttf')]:
    pdfmetrics.registerFont(TTFont(name, 'C:/Windows/Fonts/' + filename))

titles = [
    ('TITLE_01', 'Title screen / Version 1', 'Low, gruff and mysterious. Announce the title with drunken grandeur, as though presenting your greatest magical achievement.', 'The Goodnight Potion.'),
    ('TITLE_02', 'Title screen / Version 2', 'Tipsy, cheeky and delighted. Savour Potion as though you can already taste it.', 'The Goodnight Potion!'),
]
nights = [
('The Last One', 'Sleepy and mischievous. Ask the question with careless confidence, dismissing any possible consequences.', 'One last potion before bed. What harm could it do?'),
('Two for One', 'Delighted by the bargain. Finish with the certainty of a drunk who considers this excellent reasoning.', 'Two lava potions for the price of one? Better drink both!'),
('The Faster Conveyor', 'Impatient enthusiasm. Brighten on more potions, then grumble about the waiting.', 'A faster conveyor means more potions, and less waiting.'),
('It\u2019s Getting Dark', 'Briefly uneasy, then smugly reassured. A potion is obviously the answer to everything.', 'Getting dark. Fortunately, there\u2019s a potion for that.'),
('Detox', 'Hungover and gravelly. The first sentence is a reluctant admission; the second is a lazy attempt at reassurance.', 'Too much lava last night. Healing scrolls should help.'),
('The Lever', 'Bark out the opening in irritation. Follow with offended pride and a dismissive grumble.', 'Wrong direction! Never trust a machine with a wizard\u2019s job.'),
('The Bald Rogue', 'Quiet and suspicious, peering into the dark. His curiosity slightly outweighs his fear.', 'What\u2019s hiding in the darkness down here?'),
('I Can Stop Whenever I Want...', 'Defensive and stubborn. Pause between sentences, then deliver the excuse with smug conviction.', 'I can stop whenever I want. I simply don\u2019t want to.'),
('Dark Thoughts', 'Unexpectedly thoughtful, like a drunk discovering profound wisdom. Pause briefly after swallow, then warmly emphasize the light.', 'Darkness is easier to swallow after you\u2019ve seen the light.'),
('The Vortex', 'Dizzy and fascinated. Let round and round roll lazily off the tongue as he follows the movement.', 'Everything comes in pairs, going round and round.'),
('One Nail Drives Out Another', 'Firmly announce the decision to stop, then immediately soften into another convenient excuse.', 'I\u2019ll stop now. A little weed should take the edge off.'),
('Something Fishy', 'Squinting at a damaged label. Deliver the second sentence with dry resignation, hardly concerned about the missing information.', 'The label says seaweed. The rest was washed away.'),
('So Dark', 'Warm, tipsy philosophy. Emphasize a few glasses with the knowing confidence of an experienced drinker.', 'A few glasses are enough to brighten the night.'),
('It\u2019s Cold', 'Shivering and gruff at first, then visibly warming to the thought of another drink.', 'Cold nights call for a little liquid fire.'),
('One Night at a Time', 'A slightly unsteady marching rhythm on left, right. Become sleepier as the sentence approaches bedtime.', 'Left, right, left, right\u2026 one potion after another, until bedtime.'),
('The Devil Inside', 'Cold and tempted. Lower the voice conspiratorially on that devil, then finish with growing interest.', 'So cold\u2026 and that devil keeps offering to warm me up.'),
('Both Moons Shining', 'Casually choosing a drink, then distracted by the sky. Pause before Both of them, sincerely impressed by his double vision.', 'Which poison tonight? The moons look beautiful. Both of them.'),
('Shower? Nah...', 'Genuinely trying to remember. Pause, then dismiss the matter with shameless indifference.', 'When did I last shower? It can wait.'),
('Fish', 'Begin with exaggerated scholarly authority. Then falter, suddenly unsure of his own memory.', 'Apparently, fish improves your memory. Or was it seaweed?'),
('Never-Ending', 'Initially wary, then increasingly stubborn and possessive. Finish with loud, reckless, drunken defiance.', 'This stuff is terribly dark, but I must drink it all. No one can stop me!'),
]
spoken = [x[3] for x in titles] + [x[2] for x in nights]
counts = [len(re.findall(r"[A-Za-z]+(?:[\u2019'][A-Za-z]+)*", s)) for s in spoken]
assert sum(counts) == 200, counts

W, H = A4
LEFT, WIDTH = 48, W - 96
INK, MUTED, ACCENT = map(HexColor, ['#222335', '#626578', '#694D86'])
c = canvas.Canvas(str(OUT), pagesize=A4)
c.setTitle('The Goodnight Potion - English Voice Actor Script')
c.setAuthor('The Goodnight Potion')
styles = {
 'body': ParagraphStyle('body', fontName='Body', fontSize=11, leading=15, textColor=INK),
 'note': ParagraphStyle('note', fontName='Italic', fontSize=10.5, leading=14, textColor=MUTED),
 'quote': ParagraphStyle('quote', fontName='Bold', fontSize=14, leading=18, textColor=INK),
 'label': ParagraphStyle('label', fontName='Bold', fontSize=11, leading=14, textColor=ACCENT),
}
def para(text, y, style='body', x=LEFT, width=WIDTH):
    p = Paragraph(escape(text), styles[style])
    _, height = p.wrap(width, H)
    p.drawOn(c, x, y - height)
    return y - height

def page_header(section, number):
    c.setFillColor(ACCENT)
    c.setFont('Bold', 9)
    c.drawString(LEFT, H - 38, 'THE GOODNIGHT POTION  /  VOICE RECORDING SCRIPT')
    c.setFillColor(INK)
    c.setFont('Bold', 23)
    c.drawString(LEFT, H - 78, section)
    c.setStrokeColor(HexColor('#DCD7E4'))
    c.line(LEFT, H - 94, W - LEFT, H - 94)
    c.setFillColor(MUTED)
    c.setFont('Body', 9)
    c.drawString(LEFT, 30, 'ENGLISH  |  200 spoken words  |  Record quoted text only')
    c.drawRightString(W - LEFT, 30, f'{number} / 5')

def block(y, ident, title, note, line):
    y = para(ident + '  |  ' + title, y, 'label') - 9
    y = para('[' + note + ']', y, 'note') - 10
    y = para('\u201c' + line + '\u201d', y, 'quote')
    return y

page_header('Actor brief & title screen', 1)
y = H - 115
y = para('CHARACTER - applies to every line', y, 'label') - 8
y = para('The Wizard is old, gruff, rough-mannered and perpetually drunk - or, on his best nights, merely tipsy. His voice is weathered and slightly raspy, with loose articulation, uneven pauses and occasional drunken self-importance. Keep every word understandable.', y) - 10
y = para('Apply this character to every delivery note. Frightened, delighted or thoughtful, he is always the same scruffy, boozy old wizard. His wisdom is questionable, his manners are poor, and he takes his drinking very seriously.', y) - 17
y = para('WHAT TO RECORD', y, 'label') - 7
y = para('Record only text inside quotation marks. Do not read headings, IDs or acting directions. No additional spoken words or improvised lines. Total: 22 clips, 200 spoken words (6 title words + 194 night words). Contractions count as one word.', y) - 17
y = para('DELIVERY NOTES', y, 'label') - 7
y = para('Preferred format, unless agreed otherwise: mono WAV, 48 kHz / 24-bit, clean and dry, without music, reverb or character effects. Keep a consistent English accent. Leave a little clean room around each line.', y) - 8
y = para('Deliver one file per ID, for example TITLE_01.wav or NIGHT_09.wav. The two title versions are separate performances. Additional takes or pickups should be agreed separately; they are not included in this 200-word script.', y) - 22
for ident, title, note, line in titles:
    y = block(y, ident, title, note, line) - 24
assert y > 42, y
c.showPage()

for page in range(4):
    first = page * 5 + 1
    page_header(f'Night {first:02d} - Night {first+4:02d}', page + 2)
    y = H - 119
    for idx in range(page * 5, page * 5 + 5):
        title, note, line = nights[idx]
        end = block(y, f'NIGHT_{idx+1:02d}', title, note, line)
        assert end > 55, (idx, end)
        y = min(y - 132, end - 23)
    c.showPage()
c.save()

reader = PdfReader(OUT)
assert len(reader.pages) == 5
extracted = '\n'.join(p.extract_text() for p in reader.pages)
quoted = re.findall('\u201c(.*?)\u201d', extracted, re.S)
assert len(quoted) == 22, len(quoted)
pdf_count = sum(len(re.findall(r"[A-Za-z]+(?:[\u2019'][A-Za-z]+)*", s)) for s in quoted)
assert pdf_count == 200, pdf_count
print(f'Created: {OUT}\nPages: {len(reader.pages)}\nSpoken clips: {len(quoted)}\nSpoken words: {pdf_count}')
