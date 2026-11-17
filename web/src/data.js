// re-c web port — data loader (Ph7 web 2/8). DOM-free; runs under Node and in the browser.
//
// Everything the port uses comes from web/data/ (written by tools/gen_web_data.py, see
// docs/architecture_web.md §5). loadData(read) reads manifest.json, then every listed file, and checks
// each file's size against the manifest. `read(name)` returns a Promise of an ArrayBuffer / Uint8Array:
//   browser: (n) => fetch('data/' + n).then((r) => r.arrayBuffer())
//   node   : (n) => fs.promises.readFile(path.join(dataDir, n))
// Result: { manifest, bin: {name: Uint8Array}, json: {name: object}, chr, palettes, tables, ramLabels, ... }
// with the most-used files also under short keys (file name without extension, camelCase).

const camel = (name) => name.replace(/\.(bin|json)$/, '').replace(/_([a-z0-9])/g, (_, c) => c.toUpperCase());

export async function loadData(read, { only = null } = {}) {
  const manifest = JSON.parse(new TextDecoder().decode(new Uint8Array(await read('manifest.json'))));
  const out = { manifest, bin: {}, json: {} };
  const names = Object.keys(manifest.files).filter((n) => !only || only.includes(n));
  await Promise.all(names.map(async (name) => {
    const meta = manifest.files[name];
    const bytes = new Uint8Array(await read(name));
    if (meta.size !== undefined && bytes.length !== meta.size) {
      throw new Error(`web/data/${name}: ${bytes.length} bytes, manifest says ${meta.size} (rerun tools/gen_web_data.py)`);
    }
    if (name.endsWith('.json')) {
      out.json[name] = JSON.parse(new TextDecoder().decode(bytes));
      out[camel(name)] = out.json[name];
    } else {
      out.bin[name] = bytes;
      out[camel(name)] = bytes;
    }
  }));
  return out;
}

/** values of a named fixed-size ROM table from tables.json (throws if missing, so typos fail loudly) */
export function table(data, name) {
  const t = data.tables && data.tables[name];
  if (!t) throw new Error(`tables.json has no table ${name}`);
  return t.values;
}
