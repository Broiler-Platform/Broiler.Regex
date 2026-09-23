import assert from 'node:assert/strict';
import test from 'node:test';
import { chooseVersion, readVersions, resolveVersion } from './resolve-preview-version.mjs';

test('first publish uses the configured preview; later publishes increment numerically', () => {
  assert.equal(chooseVersion('0.1.0-preview.1', []), '0.1.0-preview.1');
  assert.equal(chooseVersion('0.1.0-preview.1', ['0.1.0-preview.1']), '0.1.0-preview.2');
  assert.equal(chooseVersion('0.1.0-preview.1', ['0.1.0-preview.9', '0.1.0-preview.10']), '0.1.0-preview.11');
});

test('configured preview is a floor and other release lines do not affect it', () => {
  assert.equal(chooseVersion('0.1.0-preview.4', [
    '0.1.0-preview.1', '0.2.0-preview.99', '0.1.0', '0.1.0-rc.9',
  ]), '0.1.0-preview.4');
});

test('only unused previews on the configured release line are accepted', () => {
  const published = ['0.1.0-preview.1'];
  assert.equal(chooseVersion('0.1.0-preview.1', published, { suffix: 'preview.3' }), '0.1.0-preview.3');
  assert.equal(chooseVersion('0.1.0-preview.1', published, { tag: 'v0.1.0-preview.2' }), '0.1.0-preview.2');
  for (const suffix of ['preview.1', 'rc.2', 'preview.0', 'preview.02', 'preview.2;evil']) {
    assert.throws(() => chooseVersion('0.1.0-preview.1', published, { suffix }));
  }
  for (const tag of ['v0.1.0', 'v0.1.0-rc.2', 'v0.2.0-preview.2', 'v0.1.0-preview.1']) {
    assert.throws(() => chooseVersion('0.1.0-preview.1', published, { tag }));
  }
  assert.throws(() => chooseVersion('0.1.0', published));
});

function fakeFeed(responses) {
  return async (url, options) => {
    assert.ok(options.signal);
    if (url === 'https://feed/index.json') return Response.json({
      resources: [{ '@type': 'PackageBaseAddress/3.0.0', '@id': 'https://feed/flat/' }],
    });
    assert.ok(Object.hasOwn(responses, url), `Unexpected request ${url}`);
    const response = responses[url];
    return typeof response === 'number' ? new Response(null, { status: response }) : Response.json(response);
  };
}

test('all packages contribute, including a partially published newer preview', async () => {
  const versions = await readVersions('https://feed/index.json', ['Core', 'Provider', 'New'], {}, fakeFeed({
    'https://feed/flat/core/index.json': { versions: ['0.1.0-preview.1'] },
    'https://feed/flat/provider/index.json': { versions: ['0.1.0-preview.1', '0.1.0-preview.2'] },
    'https://feed/flat/new/index.json': 404,
  }));
  assert.equal(chooseVersion('0.1.0-preview.1', versions), '0.1.0-preview.3');
});

test('feed failures and malformed responses stop publication', async () => {
  for (const response of [401, 403, 429, 500, {}, { versions: [2] }]) {
    await assert.rejects(readVersions('https://feed/index.json', ['Core'], {}, fakeFeed({
      'https://feed/flat/core/index.json': response,
    })));
  }
  await assert.rejects(readVersions('https://feed/index.json', ['Core'], {}, async () => {
    throw new Error('Network unavailable');
  }));
  await assert.rejects(readVersions('https://feed/index.json', ['Core'], {}, async () => Response.json({})));
});

const packages = [{ PackageId: 'Broiler.Regex', PackageVersion: '0.1.0-preview.1' }];

function nugetFeed(versions) {
  return async (url, options) => {
    assert.deepEqual(options.headers, {}); // Public lookup needs no feed credentials.
    if (url === 'https://api.nuget.org/v3/index.json') return Response.json({
      resources: [{ '@type': 'PackageBaseAddress/3.0.0', '@id': 'https://api.nuget.org/v3-flatcontainer/' }],
    });
    assert.equal(url, 'https://api.nuget.org/v3-flatcontainer/broiler.regex/index.json');
    return versions === null ? new Response(null, { status: 404 }) : Response.json({ versions });
  };
}

test('retired-feed preview.3 plus nuget.org preview.2 resolves cumulatively to preview.4', async () => {
  const history = { 'Broiler.Regex': ['0.1.0-preview.3'] };
  assert.equal(await resolveVersion(packages, history, {}, nugetFeed(['0.1.0-preview.2'])), '0.1.0-preview.4');
  for (const env of [
    { VERSION_SUFFIX: 'preview.3' },
    { GITHUB_EVENT_NAME: 'push', GITHUB_REF: 'refs/tags/v0.1.0-preview.3' },
  ]) {
    await assert.rejects(resolveVersion(packages, history, env, nugetFeed(['0.1.0-preview.2'])), /at least '0.1.0-preview.4'/);
  }
});

test('nuget.org and case-insensitive history contribute only to their own package and release line', async () => {
  const history = { 'broiler.regex': ['0.1.0-preview.3', '0.2.0-preview.99'], Other: ['0.1.0-preview.99'] };
  assert.equal(await resolveVersion(packages, history, {}, nugetFeed(null)), '0.1.0-preview.4');
  assert.equal(await resolveVersion(packages, history, {}, nugetFeed(['0.1.0-preview.10'])), '0.1.0-preview.11');
  assert.equal(await resolveVersion(packages, {}, {}, nugetFeed(null)), '0.1.0-preview.1');
});

test('explicit suffixes and tags must respect the cumulative sequence', async () => {
  const history = { 'Broiler.Regex': ['0.1.0-preview.3'] };
  const env = { GITHUB_EVENT_NAME: 'push', GITHUB_REF: 'refs/tags/v0.1.0-preview.4', VERSION_SUFFIX: 'preview.4' };
  assert.equal(await resolveVersion(packages, history, env, nugetFeed([])), '0.1.0-preview.4');
  await assert.rejects(resolveVersion(packages, history, { ...env, VERSION_SUFFIX: 'preview.5' }, nugetFeed([])), /must agree/);
  await assert.rejects(resolveVersion(packages, history, { ...env, GITHUB_REF: 'refs/heads/v0.1.0-preview.4' }, nugetFeed([])), /preview tag/);
});

test('invalid history and feed failures cannot silently reset the sequence', async () => {
  for (const history of [null, [], { 'Broiler.Regex': '0.1.0-preview.3' }, { 'Broiler.Regex': ['typo'] }]) {
    await assert.rejects(resolveVersion(packages, history, {}, nugetFeed([])));
  }
  await assert.rejects(resolveVersion(packages, { 'Broiler.Regex': ['0.1.0-preview.3'] }, {}, async () =>
    new Response(null, { status: 503 })), /HTTP 503/);
});
