import { readFile, access } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { guideLinks, exampleLinks } from '../docs/.vitepress/navigation.mjs';

const root = new URL('../', import.meta.url);
const paths = [
  'docs/index.md', 'docs/api/index.md', 'docs/releases/index.md',
  'docs/releases/workflow.md', 'docs/releases/scope.md', 'docs/contributing.md',
  ...guideLinks.map(([, path]) => `docs/guides/${path}.md`),
  ...exampleLinks.map(([, path]) => `docs/examples/${path}.md`)
];

for (const path of paths) {
  const url = new URL(path, root);
  const content = await readFile(url, 'utf8');
  if (!content.startsWith('# ')) throw new Error(`${path}: missing page title`);
  for (const [, source] of content.matchAll(/^<<<\s+(\S+)/gm)) {
    await access(new URL(source, url));
  }
}

console.log(`Checked ${paths.length} documentation pages in ${fileURLToPath(root)}.`);
