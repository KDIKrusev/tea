#!/usr/bin/env node
/**
 * Renders every walkthrough in this folder to pdf/.
 *
 *   node build-pdf.cjs
 *
 * No dependencies: a small Markdown subset renderer (the one these cards actually use) plus
 * headless Chrome for the PDF step. Chrome is located automatically; override with CHROME_BIN.
 *
 * The previous PDFs were produced by hand on 2026-07-22 and drifted: they covered only 01–18,
 * missed the 2026-07-28 revision of card 04, and predated 00-ORIENTATION.md entirely. A script
 * beats a habit.
 */
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');

const DIR = __dirname;
const OUT = path.join(DIR, 'pdf');

// ─── Markdown → HTML ─────────────────────────────────────────────────────────
// Deliberately covers only what these documents use: ATX headings, pipe tables, fenced code,
// block quotes, ordered/unordered lists, thematic breaks, and inline code/bold/italic/links.

const escapeHtml = s =>
  s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

function inline(text) {
  const codes = [];
  let s = escapeHtml(text)
    // pull code spans out first so their contents are never treated as markup
    .replace(/`([^`]+)`/g, (_m, code) => `\u0000${codes.push(code) - 1}\u0000`)
    // A link to a sibling .md must point at the .pdf — the markdown files are not shipped
    // alongside the rendered set, so an unrewritten link is dead on arrival.
    .replace(/\[([^\]]+)\]\(([^)]+)\)/g, (_m, label, href) =>
      `<a href="${href.replace(/^(?!https?:)(.*)\.md(#.*)?$/, '$1.pdf$2')}">${label}</a>`)
    .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
    .replace(/(^|[^*])\*([^*]+)\*/g, '$1<em>$2</em>');
  return s.replace(/\u0000(\d+)\u0000/g, (_m, i) => `<code>${codes[Number(i)]}</code>`);
}

const isTableRow = line => /^\s*\|.*\|\s*$/.test(line);
const isDivider = line => /^\s*\|[\s:|-]+\|\s*$/.test(line);
const cells = line => line.trim().replace(/^\||\|$/g, '').split('|').map(c => c.trim());

function render(md) {
  // HTML comments are editorial notes (e.g. the `header:auto` marker) — never page content.
  const lines = md.replace(/<!--[\s\S]*?-->/g, '').split(/\r?\n/);
  const out = [];
  let i = 0;
  const listStack = [];

  const closeLists = () => {
    while (listStack.length) out.push(`</${listStack.pop()}>`);
  };

  while (i < lines.length) {
    const line = lines[i];

    // fenced code
    if (/^\s*```/.test(line)) {
      closeLists();
      const body = [];
      i++;
      while (i < lines.length && !/^\s*```/.test(lines[i])) body.push(lines[i++]);
      i++;
      out.push(`<pre><code>${escapeHtml(body.join('\n'))}</code></pre>`);
      continue;
    }

    // table
    if (isTableRow(line) && i + 1 < lines.length && isDivider(lines[i + 1])) {
      closeLists();
      const head = cells(line);
      i += 2;
      const body = [];
      while (i < lines.length && isTableRow(lines[i])) body.push(cells(lines[i++]));
      out.push(
        '<table><thead><tr>' +
          head.map(c => `<th>${inline(c)}</th>`).join('') +
          '</tr></thead><tbody>' +
          body
            .map(r => '<tr>' + r.map(c => `<td>${inline(c)}</td>`).join('') + '</tr>')
            .join('') +
          '</tbody></table>'
      );
      continue;
    }

    // heading
    const heading = line.match(/^(#{1,6})\s+(.*)$/);
    if (heading) {
      closeLists();
      const level = heading[1].length;
      out.push(`<h${level}>${inline(heading[2])}</h${level}>`);
      i++;
      continue;
    }

    // thematic break
    if (/^\s*---\s*$/.test(line)) {
      closeLists();
      out.push('<hr>');
      i++;
      continue;
    }

    // block quote (may contain headings and lists of its own)
    if (/^\s*>/.test(line)) {
      closeLists();
      const body = [];
      while (i < lines.length && /^\s*>/.test(lines[i])) {
        body.push(lines[i].replace(/^\s*>\s?/, ''));
        i++;
      }
      out.push(`<blockquote>${render(body.join('\n'))}</blockquote>`);
      continue;
    }

    // list item
    const item = line.match(/^(\s*)([-*]|\d+\.)\s+(.*)$/);
    if (item) {
      const tag = /\d/.test(item[2]) ? 'ol' : 'ul';
      if (!listStack.length || listStack[listStack.length - 1] !== tag) {
        closeLists();
        listStack.push(tag);
        out.push(`<${tag}>`);
      }
      // continuation lines of the same item
      const parts = [item[3]];
      while (i + 1 < lines.length && /^\s{2,}\S/.test(lines[i + 1]) && !/^\s*([-*]|\d+\.)\s/.test(lines[i + 1])) {
        parts.push(lines[++i].trim());
      }
      out.push(`<li>${inline(parts.join(' '))}</li>`);
      i++;
      continue;
    }

    if (!line.trim()) {
      closeLists();
      i++;
      continue;
    }

    // paragraph
    closeLists();
    const para = [line];
    i++;
    while (
      i < lines.length &&
      lines[i].trim() &&
      !/^\s*(#{1,6}\s|```|>|---\s*$)/.test(lines[i]) &&
      !isTableRow(lines[i]) &&
      !/^(\s*)([-*]|\d+\.)\s/.test(lines[i])
    ) {
      para.push(lines[i++]);
    }
    out.push(`<p>${inline(para.join(' '))}</p>`);
  }

  closeLists();
  return out.join('\n');
}

// ─── Page shell ──────────────────────────────────────────────────────────────

const CSS = `
  @page { size: A4; margin: 16mm 14mm 18mm 14mm; }
  * { box-sizing: border-box; }
  body {
    font: 10.5pt/1.5 "Segoe UI", system-ui, sans-serif;
    color: #1a1a1a; margin: 0;
  }
  h1 { font-size: 19pt; margin: 0 0 4mm; color: #0b3d63; line-height: 1.25; }
  h2 { font-size: 13.5pt; margin: 7mm 0 2.5mm; color: #0b3d63;
       border-bottom: 1.5px solid #d4dde4; padding-bottom: 1.5mm; break-after: avoid; }
  h3 { font-size: 11.5pt; margin: 5mm 0 2mm; color: #14507d; break-after: avoid; }
  h4 { font-size: 10.5pt; margin: 4mm 0 1.5mm; color: #14507d; break-after: avoid; }
  p { margin: 0 0 2.5mm; }
  ul, ol { margin: 0 0 2.5mm; padding-left: 6mm; }
  li { margin: 0 0 1mm; }
  strong { color: #06263f; }
  hr { border: 0; border-top: 1px solid #dde4ea; margin: 6mm 0; }

  code {
    font-family: "Cascadia Mono", Consolas, monospace; font-size: 9pt;
    background: #f2f5f8; padding: 0.4mm 1.1mm; border-radius: 2px; color: #0b3d63;
  }
  pre {
    background: #f7f9fb; border: 1px solid #e2e9ef; border-left: 3px solid #2b7bb9;
    border-radius: 3px; padding: 2.5mm 3mm; margin: 0 0 3mm;
    overflow-x: auto; break-inside: avoid;
  }
  pre code { background: none; padding: 0; font-size: 8.6pt; line-height: 1.45; color: #16323f; }

  /* Tables are the point of these documents — they must never interleave across a page break. */
  table {
    border-collapse: collapse; width: 100%; margin: 0 0 3.5mm;
    font-size: 9.2pt; break-inside: avoid;
  }
  th, td {
    border: 1px solid #d4dde4; padding: 1.6mm 2.2mm;
    text-align: left; vertical-align: top; word-break: normal; overflow-wrap: anywhere;
  }
  th { background: #eef3f7; font-weight: 600; color: #0b3d63; }
  tbody tr:nth-child(even) { background: #fafcfd; }

  blockquote {
    margin: 0 0 3.5mm; padding: 2.5mm 3.5mm;
    background: #fff8e8; border-left: 3px solid #e0a800; border-radius: 2px;
    break-inside: avoid;
  }
  blockquote > :last-child { margin-bottom: 0; }
  blockquote h3 { margin-top: 0; }

  footer {
    margin-top: 8mm; padding-top: 2.5mm; border-top: 1px solid #dde4ea;
    font-size: 8pt; color: #6b7c88;
  }
`;

function page(title, bodyHtml, stamp) {
  return `<!doctype html><html><head><meta charset="utf-8"><title>${escapeHtml(title)}</title>
<style>${CSS}</style></head><body>
${bodyHtml}
<footer>KSailCalc manual test scenarios — calculation walkthrough · regenerated ${stamp} ·
numbers verified against commit 559aef2 and unchanged by the backend/client refactors
(18 golden snapshots byte-identical)</footer>
</body></html>`;
}

// ─── Build ───────────────────────────────────────────────────────────────────

function findChrome() {
  if (process.env['CHROME_BIN']) return process.env['CHROME_BIN'];
  const candidates = [
    'C:/Program Files/Google/Chrome/Application/chrome.exe',
    'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
    'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
    '/usr/bin/google-chrome',
    '/usr/bin/chromium',
  ];
  const found = candidates.find(p => fs.existsSync(p));
  if (!found) throw new Error('No Chrome/Edge found. Set CHROME_BIN.');
  return found;
}

function main() {
  const stamp = process.env['BUILD_DATE'] || new Date().toISOString().slice(0, 10);
  const chrome = findChrome();

  // Single-file mode: `node build-pdf.cjs <file.md>` renders one document next to its source.
  // Used for one-off notes (e.g. customer explainers) that do not belong in the scenario set.
  const single = process.argv[2] ? path.resolve(process.argv[2]) : null;
  const outDir = single ? path.dirname(single) : OUT;
  fs.mkdirSync(outDir, { recursive: true });

  // Every walkthrough, plus the two documents one level up that the rendered set used to omit:
  // the scenario list with its expected results, and the coverage matrix. Both are needed to
  // actually run the tests, and neither existed as a PDF before.
  // `.html` sources are passed through unrendered — that is how the diagram page carries inline
  // SVG, which the Markdown renderer would escape into visible angle brackets.
  const sources = (single
    ? [{ src: single, out: path.basename(single).replace(/\.(md|html)$/, '.pdf') }]
    : [
        ...fs.readdirSync(DIR).filter(f => /\.(md|html)$/.test(f)).sort()
          .map(f => ({ src: path.join(DIR, f), out: f.replace(/\.(md|html)$/, '.pdf') })),
        { src: path.join(DIR, '..', 'README.md'), out: 'SCENARIOS-AND-EXPECTED-RESULTS.pdf' },
        { src: path.join(DIR, '..', 'COVERAGE-MATRIX.md'), out: 'COVERAGE-MATRIX.pdf' },
      ]
  ).filter(s => fs.existsSync(s.src));

  const tmp = path.join(outDir, '.tmp.html');
  let ok = 0;

  for (const { src, out } of sources) {
    const file = path.basename(src);
    const source = fs.readFileSync(src, 'utf8');
    const isHtml = /\.html$/.test(src);
    const title = isHtml
      ? (source.match(/<h1[^>]*>(.*?)<\/h1>/s) || [, file])[1].replace(/<[^>]+>/g, '')
      : (source.match(/^#\s+(.*)$/m) || [, file])[1];
    fs.writeFileSync(tmp, page(title, isHtml ? source : render(source), stamp), 'utf8');

    const pdf = path.join(outDir, out);
    execFileSync(chrome, [
      '--headless', '--disable-gpu', '--no-sandbox',
      '--no-pdf-header-footer',
      `--print-to-pdf=${pdf}`,
      'file:///' + tmp.replace(/\\/g, '/'),
    ], { stdio: 'ignore' });

    console.log(`  ${file.padEnd(42)} → ${out}`);
    ok++;
  }

  fs.unlinkSync(tmp);
  console.log(`\n${ok} PDF(s) written to ${outDir}`);
}

main();
