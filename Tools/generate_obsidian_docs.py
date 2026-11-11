"""Genera un vault de Obsidian con los diagramas de clases de Alien Farms.

Uso (desde la raíz del repo):  python Tools/generate_obsidian_docs.py
Salida: Docs/Arquitectura/  (abrir esa carpeta como vault en Obsidian).

Parser por regex: no es un compilador de C#, pero cubre el estilo del proyecto
(una clase por archivo, miembros en líneas propias, [SerializeField] private).
"""
from __future__ import annotations

import html
import re
import shutil
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SCRIPTS = ROOT / "Assets" / "Scripts"
OUT = ROOT / "Docs" / "Arquitectura"
GENERATED_DIRS = ("Clases", "Modulos")
MAX_MEMBERS_IN_DIAGRAM = 14

TYPE_DECL = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*(?P<vis>public|internal|private|protected)?\s*"
    r"(?P<mods>(?:(?:static|abstract|sealed|partial|readonly|ref)\s+)*)"
    r"(?P<kind>class|interface|enum|struct|record)\s+(?P<name>\w+)(?P<gen><[^>]*>)?"
    r"(?:\s*:\s*(?P<bases>[^{/]+))?"
)
VIS = r"(?P<vis>public|private|protected|internal)"
FIELD = re.compile(
    rf"^\s*(?:\[[^\]]*\]\s*)*{VIS}?\s*(?P<mods>(?:(?:static|readonly|const|new)\s+)*)"
    r"(?P<type>[\w\.]+(?:<[\w\s,<>\.\[\]?]*>)?(?:\[\])*\??)\s+(?P<name>\w+)\s*(?:=[^;]*)?;\s*(?://.*)?$"
)
EVENT = re.compile(
    rf"^\s*{VIS}?\s*(?P<static>static\s+)?event\s+(?P<type>[\w\.]+(?:<[^;]*>)?)\s+(?P<name>\w+)\s*;"
)
PROPERTY = re.compile(
    rf"^\s*(?:\[[^\]]*\]\s*)*{VIS}\s*(?P<mods>(?:(?:static|abstract|virtual|override|sealed|new)\s+)*)"
    r"(?P<type>[\w\.]+(?:<[\w\s,<>\.\[\]?]*>)?(?:\[\])*\??)\s+(?P<name>[A-Z]\w*)\s*(?:=>|\{|$)"
)
METHOD = re.compile(
    rf"^\s*(?:\[[^\]]*\]\s*)*{VIS}?\s*(?P<mods>(?:(?:static|abstract|virtual|override|sealed|async|new|extern)\s+)*)"
    r"(?P<type>[\w\.]+(?:<[\w\s,<>\.\[\]?]*>)?(?:\[\])*\??)\s+(?P<name>\w+)\s*(?:<[^>]*>)?\((?P<params>[^)]*)\)?"
)
INTERFACE_METHOD = re.compile(
    r"^\s*(?P<type>[\w\.]+(?:<[\w\s,<>\.\[\]?]*>)?(?:\[\])*\??)\s+(?P<name>\w+)\s*\((?P<params>[^)]*)\)\s*;"
)
INTERFACE_PROP = re.compile(
    r"^\s*(?P<type>[\w\.]+(?:<[\w\s,<>\.\[\]?]*>)?(?:\[\])*\??)\s+(?P<name>[A-Z]\w*)\s*\{\s*get"
)
KEYWORDS = {
    "return", "new", "if", "else", "for", "foreach", "while", "switch", "case", "using", "namespace",
    "throw", "var", "await", "yield", "base", "this", "get", "set", "operator", "implicit", "explicit",
}
UNITY_MESSAGES = {
    "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnEnable", "OnDisable", "OnDestroy",
    "OnValidate", "Reset", "OnDrawGizmos", "OnDrawGizmosSelected", "OnTriggerEnter", "OnTriggerExit",
    "OnTriggerStay", "OnCollisionEnter", "OnCollisionExit", "OnCollisionStay", "OnGUI",
}


@dataclass
class Member:
    vis: str
    text: str           # ya formateado para Mermaid
    is_serialized: bool = False
    is_public_api: bool = False


@dataclass
class TypeInfo:
    name: str
    kind: str
    mods: set
    namespace: str
    module: str
    path: Path
    bases: list = field(default_factory=list)
    summary: str = ""
    members: list = field(default_factory=list)
    field_refs: set = field(default_factory=set)    # tipos del proyecto usados como campo/propiedad
    body_refs: set = field(default_factory=set)     # cualquier tipo del proyecto mencionado
    enum_values: list = field(default_factory=list)
    events_raised: set = field(default_factory=set)
    events_subscribed: set = field(default_factory=set)
    events_declared: list = field(default_factory=list)  # (name, payload, summary)

    @property
    def stereotype(self) -> str:
        if self.kind == "interface":
            return "interface"
        if self.kind == "enum":
            return "enumeration"
        if self.kind in ("struct", "record"):
            return "struct"
        if "static" in self.mods:
            return "static"
        if "abstract" in self.mods:
            return "abstract"
        unity = {"MonoBehaviour": "MonoBehaviour", "ScriptableObject": "ScriptableObject", "EditorWindow": "EditorWindow"}
        for b in self.bases:
            if b in unity:
                return unity[b]
        return ""


def strip_comments_keep_lines(text: str) -> str:
    """Quita comentarios y literales de string conservando saltos de línea."""
    out, i, n = [], 0, len(text)
    while i < n:
        c = text[i]
        if text.startswith("//", i):
            j = text.find("\n", i)
            j = n if j == -1 else j
            i = j
        elif text.startswith("/*", i):
            j = text.find("*/", i + 2)
            j = n if j == -1 else j + 2
            out.append("\n" * text.count("\n", i, j))
            i = j
        elif c == '"' or (c in "@$" and i + 1 < n and text[i + 1] == '"') or text.startswith('$@"', i) or text.startswith('@$"', i):
            start = i
            verbatim = False
            while text[i] != '"':
                verbatim |= text[i] == "@"
                i += 1
            i += 1
            while i < n:
                if verbatim and text.startswith('""', i):
                    i += 2
                    continue
                if not verbatim and text[i] == "\\":
                    i += 2
                    continue
                if text[i] == '"':
                    i += 1
                    break
                i += 1
            out.append('""' + "\n" * text.count("\n", start, i))
        elif c == "'" and i + 2 < n and (text[i + 2] == "'" or (text[i + 1] == "\\" and i + 3 < n and text[i + 3] == "'")):
            j = text.find("'", i + 1 + (1 if text[i + 1] == "\\" else 0) + 1)
            out.append("' '")
            i = j + 1
        else:
            out.append(c)
            i += 1
    return "".join(out)


def mermaid_type(t: str) -> str:
    t = re.sub(r"\s+", " ", t.strip())
    return t.replace("<", "~").replace(">", "~").replace("?", "")


def short_params(params: str) -> str:
    parts = []
    for p in split_top_level(params):
        p = re.sub(r"\[[^\]]*\]", "", p)            # atributos en parámetros
        p = p.split("=")[0].strip()
        p = re.sub(r"^(this|ref|out|in|params)\s+", "", p)
        if not p:
            continue
        bits = p.rsplit(" ", 1)
        parts.append(f"{mermaid_type(bits[0])} {bits[1]}" if len(bits) == 2 else mermaid_type(p))
    return ", ".join(parts)


def split_top_level(s: str) -> list:
    out, depth, cur = [], 0, []
    for ch in s:
        if ch in "<([":
            depth += 1
        elif ch in ">)]":
            depth -= 1
        if ch == "," and depth == 0:
            out.append("".join(cur))
            cur = []
        else:
            cur.append(ch)
    if "".join(cur).strip():
        out.append("".join(cur))
    return out


def vis_symbol(vis: str | None, default: str) -> str:
    return {"public": "+", "private": "-", "protected": "#", "internal": "~"}.get(vis or default, "-")


def extract_summary(raw_lines: list, decl_line: int) -> str:
    """Toma el /// <summary> inmediatamente encima de la declaración."""
    buf = []
    i = decl_line - 1
    while i >= 0 and (raw_lines[i].strip().startswith("///") or raw_lines[i].strip().startswith("[")):
        if raw_lines[i].strip().startswith("///"):
            buf.append(raw_lines[i].strip()[3:].strip())
        i -= 1
    text = " ".join(reversed(buf))
    m = re.search(r"<summary>(.*?)</summary>", text)
    text = m.group(1) if m else text
    text = re.sub(r'<see cref="([^"]+)"\s*/>', r"`\1`", text)
    text = re.sub(r"<paramref name=\"([^\"]+)\"\s*/>", r"`\1`", text)
    text = re.sub(r"<[^>]+>", "", text)
    return html.unescape(re.sub(r"\s+", " ", text).strip())


def parse_file(path: Path) -> list:
    raw = path.read_text(encoding="utf-8-sig")
    raw_lines = raw.splitlines()
    code_lines = strip_comments_keep_lines(raw).splitlines()
    rel = path.relative_to(SCRIPTS)
    module = rel.parts[0] if len(rel.parts) > 1 else "Root"

    ns_match = re.search(r"namespace\s+([\w\.]+)", "\n".join(code_lines))
    namespace = ns_match.group(1) if ns_match else ""

    types, stack = [], []   # stack: (TypeInfo, depth_of_body)
    depth = 0
    pending_attr = ""
    for idx, line in enumerate(code_lines):
        stripped = line.strip()
        current = stack[-1][0] if stack else None
        at_member_level = stack and depth == stack[-1][1]

        decl = TYPE_DECL.match(line)
        if decl and (not stack or at_member_level or depth <= (stack[-1][1] if stack else 0)):
            t = TypeInfo(
                name=decl.group("name"), kind=decl.group("kind"), mods=set(decl.group("mods").split()),
                namespace=namespace, module=module, path=path,
                summary=extract_summary(raw_lines, idx),
            )
            if decl.group("bases"):
                t.bases = [re.sub(r"<.*", "", b).strip() for b in split_top_level(decl.group("bases")) if b.strip()]
                t.bases = [b.split(" where")[0].strip() for b in t.bases]
            types.append(t)
            stack.append((t, depth + 1))
            pending_attr = ""
        elif current is not None and at_member_level and stripped:
            if stripped.startswith("["):
                pending_attr += stripped
            serialized = "SerializeField" in pending_attr or "SerializeField" in stripped or "SerializeReference" in stripped
            parse_member(current, stripped, serialized)
            if not stripped.startswith("["):
                pending_attr = ""
        if current is not None and current.kind == "enum" and at_member_level and stripped:
            for v in re.findall(r"^\s*(\w+)\s*(?:=\s*[^,]+)?,?", line):
                if v not in current.enum_values:
                    current.enum_values.append(v)

        for ch in line:
            if ch == "{":
                depth += 1
            elif ch == "}":
                depth -= 1
                while stack and depth < stack[-1][1]:
                    stack.pop()

        # eventos globales: quién publica / quién se suscribe
        for t, _ in stack[-1:]:
            for ev in re.findall(r"GameEvents\.(On\w+)\s*[+-]=", line):
                t.events_subscribed.add(ev)
            for ev in re.findall(r"GameEvents\.Raise(\w+)\s*\(", line):
                t.events_raised.add("On" + ev)

    # cuerpo completo por tipo (para referencias)
    for t in types:
        t._body = "\n".join(code_lines)  # type: ignore[attr-defined]
    if namespace.endswith(".Core") and any(t.name == "GameEvents" for t in types):
        ge = next(t for t in types if t.name == "GameEvents")
        for i, l in enumerate(code_lines):
            m = EVENT.match(l)
            if m:
                ge.events_declared.append((m.group("name"), m.group("type"), extract_summary(raw_lines, i)))
    return types


def parse_member(t: TypeInfo, line: str, serialized: bool) -> None:
    clean = re.sub(r"^(\[[^\]]*\]\s*)+", "", line)
    if not clean or clean.startswith(("{", "}", "//", "#")):
        return
    if t.kind == "enum":
        return

    m = EVENT.match(clean)
    if m:
        static = "$" if m.group("static") else ""
        t.members.append(Member(vis_symbol(m.group("vis"), "private"),
                                f"{vis_symbol(m.group('vis'), 'private')}event {mermaid_type(m.group('type'))} {m.group('name')}{static}",
                                is_public_api=m.group("vis") == "public"))
        return

    if t.kind == "interface":
        m = INTERFACE_METHOD.match(clean)
        if m and m.group("type") not in KEYWORDS:
            t.members.append(Member("+", f"+{m.group('name')}({short_params(m.group('params'))}) {mermaid_type(m.group('type'))}", is_public_api=True))
            t.field_refs.add(m.group("type"))
            return
        m = INTERFACE_PROP.match(clean)
        if m:
            t.members.append(Member("+", f"+{mermaid_type(m.group('type'))} {m.group('name')}", is_public_api=True))
            t.field_refs.add(m.group("type"))
        return

    m = FIELD.match(clean)
    if m and m.group("type") not in KEYWORDS and "(" not in clean.split("=")[0]:
        mods = m.group("mods").split()
        vis = vis_symbol(m.group("vis"), "private")
        suffix = "$" if "static" in mods or "const" in mods else ""
        if "const" in mods:
            return  # constantes: ruido en el diagrama
        t.members.append(Member(vis, f"{vis}{mermaid_type(m.group('type'))} {m.group('name')}{suffix}",
                                is_serialized=serialized, is_public_api=m.group("vis") == "public"))
        t.field_refs.add(m.group("type"))
        return

    m = METHOD.match(clean)
    if m and m.group("type") not in KEYWORDS and m.group("name") not in KEYWORDS:
        mods = m.group("mods").split()
        name = m.group("name")
        if name == t.name:
            return  # constructor
        vis = vis_symbol(m.group("vis"), "private")
        if vis == "-" and name in UNITY_MESSAGES:
            return
        suffix = "$" if "static" in mods else ("*" if "abstract" in mods else "")
        t.members.append(Member(vis, f"{vis}{name}({short_params(m.group('params') or '')}){suffix} {mermaid_type(m.group('type'))}",
                                is_public_api=vis in "+#"))
        return

    m = PROPERTY.match(clean)
    if m and m.group("type") not in KEYWORDS:
        mods = m.group("mods").split()
        vis = vis_symbol(m.group("vis"), "private")
        suffix = "$" if "static" in mods else ("*" if "abstract" in mods else "")
        t.members.append(Member(vis, f"{vis}{mermaid_type(m.group('type'))} {m.group('name')}{suffix}", is_public_api=vis in "+#"))
        t.field_refs.add(m.group("type"))


def project_refs(type_text: str, known: set) -> set:
    return {w for w in re.findall(r"\b[A-Z]\w*\b", type_text) if w in known}


def resolve_refs(types: list) -> None:
    known = {t.name for t in types}
    by_file = defaultdict(list)
    for t in types:
        by_file[t.path].append(t)
    for t in types:
        refs = set()
        for ft in t.field_refs:
            refs |= project_refs(ft, known)
        t.field_refs = refs - {t.name}
        body = t._body  # type: ignore[attr-defined]
        siblings = {s.name for s in by_file[t.path]}
        mentioned = {w for w in re.findall(r"\b[A-Z]\w*\b", body) if w in known}
        # en archivos con varios tipos, el cuerpo es compartido: no atribuir hermanos a todos
        t.body_refs = (mentioned - {t.name} - (siblings if len(siblings) > 1 else set())) | t.field_refs | set(b for b in t.bases if b in known)


def diagram_for(types: list, all_by_name: dict, show_external: bool = True) -> str:
    names = {t.name for t in types}
    lines = ["```mermaid", "classDiagram", "  direction LR"]
    external = set()
    for t in sorted(types, key=lambda x: x.name):
        members = choose_members(t)
        lines.append(f"  class {t.name} {{")
        if t.stereotype:
            lines.append(f"    <<{t.stereotype}>>")
        if t.kind == "enum":
            lines += [f"    {v}" for v in t.enum_values[:12]]
        lines += [f"    {m.text}" for m in members]
        lines.append("  }")
    rels = set()
    for t in types:
        for b in t.bases:
            if b in all_by_name:
                arrow = "..|>" if all_by_name[b].kind == "interface" else "--|>"
                rels.add(f"  {t.name} {arrow} {b}")
                if b not in names:
                    external.add(b)
        for r in sorted(t.field_refs):
            if r in t.bases or all_by_name[r].kind == "enum":
                continue
            rels.add(f"  {t.name} --> {r}")
            if r not in names:
                external.add(r)
    if show_external:
        for e in sorted(external):
            et = all_by_name[e]
            lines.append(f"  class {e}:::external")
            if et.stereotype:
                lines.append(f"  <<{et.stereotype}>> {e}")
    lines += sorted(rels)
    lines.append("  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555")
    lines.append("```")
    return "\n".join(lines)


def choose_members(t: TypeInfo) -> list:
    picked = [m for m in t.members if m.is_serialized or m.is_public_api or t.kind == "interface"]
    if len(picked) > MAX_MEMBERS_IN_DIAGRAM:
        picked = picked[:MAX_MEMBERS_IN_DIAGRAM - 1] + [Member("", f"...{len(picked) - MAX_MEMBERS_IN_DIAGRAM + 1} más")]
    return picked


def link(name: str) -> str:
    return f"[[{name}]]"


def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def main() -> None:
    files = sorted(p for p in SCRIPTS.rglob("*.cs"))
    types = [t for f in files for t in parse_file(f)]
    dupes = {n for n in (t.name for t in types) if sum(1 for x in types if x.name == n) > 1}
    if dupes:
        print("AVISO: nombres de tipo duplicados (se fusionarán links):", ", ".join(sorted(dupes)))
    resolve_refs(types)
    by_name = {t.name: t for t in types}
    by_module = defaultdict(list)
    for t in types:
        by_module[t.module].append(t)

    used_by = defaultdict(set)
    for t in types:
        for r in t.body_refs:
            used_by[r].add(t.name)

    for d in GENERATED_DIRS:
        shutil.rmtree(OUT / d, ignore_errors=True)

    # --- notas por clase ---
    for t in types:
        rel = t.path.relative_to(ROOT).as_posix()
        fm = [
            "---",
            f"modulo: {t.module}",
            f"namespace: {t.namespace}",
            f"tipo: {t.stereotype or t.kind}",
            f"archivo: \"{rel}\"",
            f"tags: [clase, modulo/{t.module.lower()}]",
            "---",
        ]
        body = [f"# {t.name}", ""]
        if t.summary:
            body += [f"> {t.summary}", ""]
        body += [f"**Módulo:** [[Modulo {t.module}|{t.module}]] · **Namespace:** `{t.namespace}` · **Archivo:** `{rel}`", ""]
        if t.bases:
            body += ["## Hereda / implementa", ", ".join(link(b) if b in by_name else f"`{b}`" for b in t.bases), ""]
        subs = sorted(x.name for x in types if t.name in x.bases)
        if subs:
            body += ["## Derivados / implementaciones", ", ".join(link(s) for s in subs), ""]
        body += ["## Diagrama", diagram_for([t], by_name), ""]
        if t.kind == "enum" and t.enum_values:
            body += ["## Valores", ", ".join(f"`{v}`" for v in t.enum_values), ""]
        deps = sorted(t.body_refs)
        if deps:
            body += ["## Usa", ", ".join(link(d) for d in deps), ""]
        users = sorted(used_by[t.name] - {t.name})
        if users:
            body += ["## Usado por", ", ".join(link(u) for u in users), ""]
        if t.events_raised:
            body += ["## Eventos que publica", ", ".join(f"[[Eventos GameEvents#{e}|{e}]]" for e in sorted(t.events_raised)), ""]
        if t.events_subscribed:
            body += ["## Eventos que escucha", ", ".join(f"[[Eventos GameEvents#{e}|{e}]]" for e in sorted(t.events_subscribed)), ""]
        write(OUT / "Clases" / t.module / f"{t.name}.md", "\n".join(fm + body))

    # --- notas por módulo ---
    module_deps = defaultdict(lambda: defaultdict(int))
    for t in types:
        for r in t.body_refs:
            other = by_name[r].module
            if other != t.module:
                module_deps[t.module][other] += 1

    for mod, mtypes in sorted(by_module.items()):
        main_types = [t for t in mtypes if t.kind != "enum" or t.field_refs]
        lines = [
            "---", f"tags: [modulo, modulo/{mod.lower()}]", "---",
            f"# Módulo {mod}", "",
            f"Carpeta `Assets/Scripts/{mod}/` · {len(mtypes)} tipos. Las cajas punteadas son tipos de otros módulos.", "",
            "Leyenda: `--|>` hereda · `..|>` implementa · `-->` tiene un campo/propiedad de ese tipo. "
            "Se muestran campos `[SerializeField]` y la API pública.", "",
            "## Diagrama de clases", diagram_for(mtypes, by_name), "",
            "## Tipos", "",
            "| Tipo | Clase | Descripción |", "|---|---|---|",
        ]
        for t in sorted(mtypes, key=lambda x: x.name):
            desc = (t.summary[:140] + "…") if len(t.summary) > 140 else t.summary
            lines.append(f"| {link(t.name)} | {t.stereotype or t.kind} | {desc.replace('|', '/')} |")
        if module_deps[mod]:
            lines += ["", "## Depende de", ", ".join(f"[[Modulo {m}|{m}]] ({n})" for m, n in sorted(module_deps[mod].items()))]
        write(OUT / "Modulos" / f"Modulo {mod}.md", "\n".join(lines))

    # --- eventos ---
    ge = by_name.get("GameEvents")
    if ge:
        lines = ["---", "tags: [eventos]", "---", "# Eventos globales (GameEvents)", "",
                 "Comunicación desacoplada entre sistemas (`AlienFarms.Core.GameEvents`). "
                 "Publica = llama a `GameEvents.RaiseX`; escucha = se suscribe a `GameEvents.OnX`.", ""]
        flow = ["```mermaid", "flowchart LR"]
        for name, payload, summary in ge.events_declared:
            pubs = sorted(t.name for t in types if name in t.events_raised)
            subs = sorted(t.name for t in types if name in t.events_subscribed)
            lines += [f"## {name}", f"`{payload}`", ""]
            if summary:
                lines += [f"> {summary}", ""]
            lines += [f"- **Publica:** {', '.join(map(link, pubs)) or '—'}",
                      f"- **Escucha:** {', '.join(map(link, subs)) or '—'}", ""]
            for p in pubs:
                for s in subs:
                    flow.append(f"  {p} -- {name} --> {s}")
        flow.append("```")
        intro_end = 7
        lines[intro_end:intro_end] = ["## Mapa publicador → suscriptor", *flow, ""]
        write(OUT / "Eventos GameEvents.md", "\n".join(lines))

    # --- índice ---
    lines = ["---", "tags: [indice]", "---", "# Alien Farms — Arquitectura del código", "",
             "Vault generado automáticamente por `Tools/generate_obsidian_docs.py` desde `Assets/Scripts`. "
             "**No edites** `Clases/` ni `Modulos/` a mano: se regeneran. Para notas propias crea archivos fuera de esas carpetas.", "",
             "Tip: abre la **vista de grafo** (Ctrl+G) para ver las dependencias entre clases.", "",
             "## Dependencias entre módulos", "```mermaid", "flowchart LR"]
    for mod in sorted(by_module):
        lines.append(f"  {mod}[\"{mod} ({len(by_module[mod])})\"]")
    for mod, deps in sorted(module_deps.items()):
        for other, n in sorted(deps.items()):
            lines.append(f"  {mod} -->|{n}| {other}")
    lines += ["```", "", "## Módulos", ""]
    for mod in sorted(by_module):
        lines.append(f"- [[Modulo {mod}]] — {len(by_module[mod])} tipos")
    lines += ["", "## Transversal", "- [[Eventos GameEvents]] — quién publica y quién escucha cada evento global", "",
              "## Jerarquías principales", diagram_hierarchies(types, by_name)]
    write(OUT / "00 Indice.md", "\n".join(lines))
    print(f"OK: {len(types)} tipos, {len(by_module)} módulos -> {OUT}")


def diagram_hierarchies(types: list, by_name: dict) -> str:
    """Solo herencia/implementación entre tipos del proyecto (vista de alto nivel)."""
    lines = ["```mermaid", "classDiagram", "  direction BT"]
    involved = set()
    rels = []
    for t in types:
        for b in t.bases:
            if b in by_name:
                arrow = "..|>" if by_name[b].kind == "interface" else "--|>"
                rels.append(f"  {t.name} {arrow} {b}")
                involved |= {t.name, b}
    for n in sorted(involved):
        st = by_name[n].stereotype
        lines.append(f"  class {n}" + (f"{{\n    <<{st}>>\n  }}" if st else ""))
    lines += sorted(rels)
    lines.append("```")
    return "\n".join(lines)


if __name__ == "__main__":
    main()
