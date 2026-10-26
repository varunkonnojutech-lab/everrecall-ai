import { useCallback, useEffect, useState } from "react";

interface MemoryEvent {
  id: number;
  occurredAt: string;
  actor: string;
  action: string;
  objectName: string;
  location: string;
  room: string;
  confidence: number;
  source: string;
  evidenceNote: string;
}

interface AskResponse {
  question: string;
  answer: string;
  confidence: number;
  found: boolean;
  matchedEvent: MemoryEvent | null;
}

interface GuardianReport {
  evaluatedAt: string;
  allClear: boolean;
  items: { name: string; status: string; detail: string }[];
  alerts: string[];
}

const SAMPLE_QUESTIONS = [
  "Where are my keys?",
  "Did I take my morning pill?",
  "Did I lock the front door?",
  "Did I turn off the stove?",
  "Who came to the door?",
  "Where is my wallet?",
  "Did I water the plants?",
];

const fmtTime = (iso: string) =>
  new Date(iso).toLocaleTimeString([], { hour: "numeric", minute: "2-digit" });

export default function App() {
  const [question, setQuestion] = useState("");
  const [askResult, setAskResult] = useState<AskResponse | null>(null);
  const [asking, setAsking] = useState(false);
  const [events, setEvents] = useState<MemoryEvent[]>([]);
  const [guardian, setGuardian] = useState<GuardianReport | null>(null);
  const [forgetMinutes, setForgetMinutes] = useState(60);
  const [forgetMsg, setForgetMsg] = useState("");

  const refresh = useCallback(async () => {
    const [ev, g] = await Promise.all([
      fetch("/api/events").then((r) => r.json()),
      fetch("/api/guardian/status").then((r) => r.json()),
    ]);
    setEvents(ev);
    setGuardian(g);
  }, []);

  useEffect(() => {
    refresh().catch(() => {});
  }, [refresh]);

  const ask = async (q: string) => {
    if (!q.trim()) return;
    setAsking(true);
    try {
      const res = await fetch("/api/ask", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ question: q }),
      });
      setAskResult(await res.json());
    } finally {
      setAsking(false);
    }
  };

  const forget = async () => {
    const res = await fetch("/api/memory/forget", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ minutes: forgetMinutes }),
    });
    const data = await res.json();
    setForgetMsg(`Forgot ${data.deletedCount} event(s) from the last ${data.requestedMinutes} minutes.`);
    setAskResult(null);
    await refresh();
  };

  return (
    <div className="wrap">
      <header>
        <h1>🧠 EverRecall</h1>
        <p>Your second memory. A camera sees your day — EverRecall remembers it, so you can just ask.</p>
      </header>

      <div className="grid">
        <div className="card full">
          <h2>Ask your memory</h2>
          <div className="row">
            <input
              type="text"
              placeholder='Try "Where are my keys?"'
              value={question}
              onChange={(e) => setQuestion(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && ask(question)}
            />
            <button disabled={asking} onClick={() => ask(question)}>
              {asking ? "Remembering…" : "Ask"}
            </button>
          </div>
          <div className="chips">
            {SAMPLE_QUESTIONS.map((q) => (
              <button key={q} className="chip" onClick={() => { setQuestion(q); ask(q); }}>
                {q}
              </button>
            ))}
          </div>
          {askResult && (
            <div className={`answer ${askResult.found ? "" : "miss"}`}>
              <div>{askResult.answer}</div>
              {askResult.found && (
                <div className="meta">
                  Confidence {(askResult.confidence * 100).toFixed(0)}%
                  {askResult.matchedEvent && <> · matched event at {fmtTime(askResult.matchedEvent.occurredAt)}</>}
                </div>
              )}
              {askResult.matchedEvent && (
                <div className="ev">
                  📷 Evidence: {askResult.matchedEvent.evidenceNote}
                  <br />
                  <span className="small">
                    {askResult.matchedEvent.actor} · {askResult.matchedEvent.action.replace("_", " ")} ·{" "}
                    {askResult.matchedEvent.objectName} · {askResult.matchedEvent.location} ({askResult.matchedEvent.room})
                  </span>
                </div>
              )}
            </div>
          )}
        </div>

        <div className="card">
          <h2>Today&apos;s timeline</h2>
          <ul className="timeline">
            {events.map((e) => (
              <li key={e.id}>
                <span className="t">{fmtTime(e.occurredAt)}</span>
                {e.actor} {e.action.replace("_", " ")} <b>{e.objectName}</b> — {e.location}
                <div className="small">{e.evidenceNote}</div>
              </li>
            ))}
            {events.length === 0 && <li className="small">No memories yet today.</li>}
          </ul>
        </div>

        <div className="card">
          <h2>Checklist Guardian</h2>
          {guardian ? (
            <>
              {guardian.items.map((i) => (
                <div key={i.name} style={{ marginBottom: 8, fontSize: 14 }}>
                  <span className={`status ${i.status}`}>{i.status.toUpperCase()}</span>
                  <b>{i.name}</b>
                  <div className="small">{i.detail}</div>
                </div>
              ))}
              {guardian.allClear ? (
                <div className="clearbox">✅ All clear — nothing needs attention.</div>
              ) : (
                <div className="alertbox">⚠️ {guardian.alerts.join(" · ")}</div>
              )}
            </>
          ) : (
            <p className="small">Loading…</p>
          )}

          <h2 style={{ marginTop: 20 }}>Privacy — forget</h2>
          <p className="small">
            Your memory belongs to you. Forgetting really deletes the events — no undo.
          </p>
          <div className="row">
            <input
              type="number"
              min={1}
              value={forgetMinutes}
              onChange={(e) => setForgetMinutes(Number(e.target.value))}
            />
            <button className="danger" onClick={forget}>
              Forget last {forgetMinutes} min
            </button>
          </div>
          {forgetMsg && <p className="small">{forgetMsg}</p>}
        </div>
      </div>
    </div>
  );
}
