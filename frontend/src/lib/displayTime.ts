// Presentation only: stored civil values and second precision stay unchanged.
export function displayDate(value: string): string {
  return value.slice(0, 10).split("-").reverse().join("/");
}

export function displayDateTime(value: string): string {
  return displayDate(value) + (value.includes("T") ? " " + value.slice(11, 16) : "");
}

export function displayDuration(seconds: number): string {
  const minutes = Math.floor(Math.abs(seconds) / 60);
  return (seconds < 0 ? "−" : "") + String(Math.floor(minutes / 60)).padStart(2, "0") + ":" + String(minutes % 60).padStart(2, "0");
}

export function displayInstant(value: string): string {
  return new Date(value).toLocaleString("pt-BR", {
    day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit", hourCycle: "h23"
  });
}

export function displayTemporalText(value: string): string {
  return value.replace(/\b\d{4}-\d{2}-\d{2}(?:T\d{2}:\d{2}:\d{2}(?:\.\d+)?)?\b/g, displayDateTime)
    .replace(/\b(\d{2,3}:[0-5]\d):[0-5]\d\b/g, "$1");
}
