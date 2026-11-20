import { defineConfig } from 'vitepress';
import { apiGroups, slug, guideLinks, exampleLinks } from './navigation.mjs';

const base = process.env.DOCS_BASE || '/';
if (!base.startsWith('/') || !base.endsWith('/')) {
  throw new Error('DOCS_BASE must start and end with /.');
}

export default defineConfig({
  title: 'Schema',
  description: 'Schema validation for Aras Innovator Server Methods and C# applications.',
  base,
  cleanUrls: false,
  lastUpdated: false,
  head: [['meta', { name: 'theme-color', content: '#826019' }]],
  themeConfig: {
    logo: undefined,
    nav: [
      { text: 'Guides', link: '/guides/introduction' },
      { text: 'API', link: '/api/' },
      { text: 'Examples', link: '/examples/part' },
      { text: 'Releases', link: '/releases/' }
    ],
    sidebar: [
      { text: 'Start here', items: [
        { text: 'Overview', link: '/' },
        ...guideLinks.map(([text, path]) => ({ text, link: `/guides/${path}` }))
      ] },
      { text: 'API reference', collapsed: true, items: [
        { text: 'Overview', link: '/api/' },
        ...apiGroups.map(([text, names]) => ({ text, collapsed: true,
          items: names.map(name => ({ text: name, link: `/api/${slug(name)}` }))
        }))
      ] },
      { text: 'Examples', collapsed: true, items: exampleLinks.map(([text, path]) =>
        ({ text, link: `/examples/${path}` })) },
      { text: 'Project', collapsed: true, items: [
        { text: 'Releases', link: '/releases/' },
        { text: 'Release workflow', link: '/releases/workflow' },
        { text: 'Supported API', link: '/releases/scope' },
        { text: 'Contributing', link: '/contributing' }
      ] }
    ],
    search: { provider: 'local' },
    outline: { level: [2, 3], label: 'On this page' },
    socialLinks: [{ icon: 'github', link: 'https://github.com/InnovatorStack/Schema' }],
    docFooter: { prev: 'Previous', next: 'Next' }
  },
  markdown: { lineNumbers: false },
  sitemap: undefined
});
