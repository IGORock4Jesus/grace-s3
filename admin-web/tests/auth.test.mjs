import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import vm from "node:vm";
import ts from "typescript";
import * as axios from "axios";
import * as router from "react-router";

const source = ts.transpileModule(
  readFileSync(new URL("../app/lib/auth-loader.ts", import.meta.url), "utf8"),
  { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } },
).outputText;
const exports = {};
vm.runInNewContext(source, {
  exports, URL,
  require(name) {
    if (name === "axios") return axios;
    if (name === "react-router") return router;
    throw Error(`Unexpected import: ${name}`);
  },
});
const { withAuthLoader } = exports;
const args = { request: new Request("https://admin.test/files?sort=name") };
function httpError(status) {
  return new axios.AxiosError("Request failed", undefined, undefined, undefined, { status });
}

test("successful loader preserves its result and request", async () => {
  const user = { id: "admin" };
  const result = await withAuthLoader(async value => {
    assert.equal(value, args);
    return user;
  })(args);
  assert.equal(result, user);
});

test("parallel unauthorized loaders both request full navigation, preserving the destination", async () => {
  const loader = withAuthLoader(async () => { throw httpError(401); });
  const results = await Promise.allSettled([loader(args), loader(args)]);
  for (const result of results) {
    assert.equal(result.status, "rejected");
    assert.ok(result.reason instanceof Response);
    assert.equal(result.reason.status, 302);
    assert.equal(result.reason.headers.get("X-Remix-Reload-Document"), "true");
    assert.equal(result.reason.headers.get("Location"), "/auth/login?returnUrl=%2Ffiles%3Fsort%3Dname");
  }
});

for (const [name, error] of [
  ["forbidden", httpError(403)], ["server failure", httpError(500)],
  ["network failure", new axios.AxiosError("Network Error")],
  ["cancellation", new axios.CanceledError()], ["unexpected error", new Error("bug")],
]) {
  test(`${name} is not disguised as an expired session`, async () => {
    await assert.rejects(withAuthLoader(async () => { throw error; })(args), value => value === error);
  });
}
