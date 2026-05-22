import { mkdtemp, readFile, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { basename, join } from "node:path";
import { pathToFileURL } from "node:url";

const submitKey = {
  Enter: "Enter",
  CtrlEnter: "Ctrl + Enter",
  ShiftEnter: "Shift + Enter",
  AltEnter: "Alt + Enter",
  MetaEnter: "Meta + Enter",
} as const;

const placeholderObject = {
  chat: "{chat}",
  message: "{message}",
  prompt: "{prompt}",
  mask: "{mask}",
} as const;

const localePath = process.argv[2];

if (!localePath) {
  console.error("Usage: npx tsx scripts/parse-locale.ts <locale-file>");
  process.exit(1);
}

main().catch((error: unknown) => {
  console.error(error);
  process.exit(1);
});

async function main(): Promise<void> {
  const locale = await loadLocale(localePath);
  const flattened = flatten(locale);
  process.stdout.write(JSON.stringify(sortObject(flattened), undefined, 2));
  process.stdout.write("\n");
}

async function loadLocale(path: string): Promise<unknown> {
  const source = await readFile(path, "utf8");
  const tempDirectory = await mkdtemp(join(tmpdir(), "arcchat-locale-"));
  const modulePath = join(tempDirectory, `${basename(path, ".ts")}.ts`);

  const moduleSource = [
    "type LocaleType = any;",
    "type PartialLocaleType = any;",
    "const SAAS_CHAT_UTM_URL = \"https://nextchat.club?utm=github\";",
    "const SubmitKey = { Enter: \"Enter\", CtrlEnter: \"Ctrl + Enter\", ShiftEnter: \"Shift + Enter\", AltEnter: \"Alt + Enter\", MetaEnter: \"Meta + Enter\" } as const;",
    "function getClientConfig() { return { isApp: true }; }",
    stripImports(source),
  ].join("\n");

  try {
    await writeFile(modulePath, moduleSource, "utf8");
    const loaded = await import(`${pathToFileURL(modulePath).href}?cache=${Date.now()}`);
    return loaded.default;
  } finally {
    await rm(tempDirectory, { force: true, recursive: true });
  }
}

function stripImports(source: string): string {
  const output: string[] = [];
  let inImport = false;

  for (const line of source.split(/\r?\n/)) {
    const trimmed = line.trim();
    if (inImport) {
      inImport = !trimmed.endsWith(";");
      continue;
    }

    if (trimmed.startsWith("import ")) {
      inImport = !trimmed.endsWith(";");
      continue;
    }

    output.push(line);
  }

  return output.join("\n");
}

function flatten(value: unknown, prefix = ""): Record<string, string> {
  const flattened: Record<string, string> = {};

  if (!isRecord(value)) {
    return flattened;
  }

  for (const key of Object.keys(value).sort()) {
    const child = value[key];
    const childKey = prefix ? `${prefix}.${key}` : key;

    if (typeof child === "function") {
      flattened[childKey] = stringifyLeaf(invokeLocaleFunction(child));
      continue;
    }

    if (isRecord(child)) {
      Object.assign(flattened, flatten(child, childKey));
      continue;
    }

    if (Array.isArray(child)) {
      flattened[childKey] = JSON.stringify(child);
      continue;
    }

    flattened[childKey] = stringifyLeaf(child);
  }

  return flattened;
}

function invokeLocaleFunction(value: (...args: unknown[]) => unknown): unknown {
  const parameterNames = extractParameterNames(value);
  const argumentsForFunction = parameterNames.map(createPlaceholderArgument);
  return value(...argumentsForFunction);
}

function extractParameterNames(value: Function): string[] {
  const source = value.toString().replace(/\/\*[\s\S]*?\*\/|\/\/.*$/gm, "");
  const arrowMatch = source.match(/^(?:async\s*)?\(?\s*([^)=]*)\s*\)?\s*=>/);
  const methodMatch = source.match(/^[^(]*\(([^)]*)\)/);
  const rawParameters = arrowMatch?.[1] ?? methodMatch?.[1] ?? "";
  const parameters = rawParameters
    .split(",")
    .map((parameter) => parameter.replace(/=.*$/u, "").replace(/^\.\.\./u, "").trim())
    .filter(Boolean);

  if (parameters.length > 0) {
    return parameters;
  }

  return Array.from({ length: value.length }, (_, index) => `arg${index + 1}`);
}

function createPlaceholderArgument(name: string): unknown {
  if (name === "overview") {
    return placeholderObject;
  }

  if (name === "readonly") {
    return false;
  }

  if (name.toLowerCase() === "submitkey") {
    return submitKey.Enter;
  }

  return `{${name}}`;
}

function stringifyLeaf(value: unknown): string {
  if (typeof value === "string") {
    return value;
  }

  if (value === undefined) {
    return "";
  }

  if (typeof value === "number" || typeof value === "boolean" || value === null) {
    return String(value);
  }

  return JSON.stringify(value);
}

function sortObject(value: Record<string, string>): Record<string, string> {
  const sorted: Record<string, string> = {};
  for (const key of Object.keys(value).sort()) {
    sorted[key] = value[key];
  }

  return sorted;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}
