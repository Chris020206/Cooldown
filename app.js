const EVENTS = [
  {
    id: "valorant-champs-2025",
    title: "VALORANT Champions 2025",
    game: "VALORANT",
    region: "Global",
    major: true,
    organizer: "Riot Games",
    stage: "Grand Finals",
    format: "Best of 5",
    startTime: "2025-11-08T18:00:00Z",
    endTime: "2025-11-08T22:30:00Z",
    venue: "Seoul, South Korea",
    streams: [
      { label: "Twitch", url: "https://twitch.tv/valorant_esports" },
      { label: "YouTube", url: "https://youtube.com/valorant" }
    ]
  },
  {
    id: "league-worlds-2025",
    title: "League of Legends World Championship",
    game: "League of Legends",
    region: "Global",
    major: true,
    organizer: "Riot Games",
    stage: "Swiss Stage Week 2",
    format: "Best of 3",
    startTime: "2025-11-15T11:00:00Z",
    endTime: "2025-11-15T21:00:00Z",
    venue: "Berlin, Germany",
    streams: [
      { label: "LoL Esports", url: "https://lolesports.com/live" },
      { label: "YouTube", url: "https://youtube.com/lolesports" }
    ]
  },
  {
    id: "dota-ti-2025",
    title: "The International 2025",
    game: "Dota 2",
    region: "Asia",
    major: true,
    organizer: "Valve",
    stage: "Main Event Day 1",
    format: "Best of 3",
    startTime: "2025-12-05T16:00:00Z",
    endTime: "2025-12-05T23:00:00Z",
    venue: "Singapore Indoor Stadium",
    streams: [
      { label: "Twitch", url: "https://twitch.tv/dota2ti" },
      { label: "YouTube", url: "https://youtube.com/dota2" }
    ]
  },
  {
    id: "rlcs-winter-major",
    title: "RLCS Winter Major",
    game: "Rocket League",
    region: "North America",
    major: true,
    organizer: "Psyonix",
    stage: "Group Stage",
    format: "Swiss",
    startTime: "2025-12-12T17:00:00Z",
    endTime: "2025-12-12T22:00:00Z",
    venue: "Dallas, USA",
    streams: [{ label: "Twitch", url: "https://twitch.tv/rocketleague" }]
  },
  {
    id: "cs2-major-2025",
    title: "CS2 Copenhagen Major",
    game: "Counter-Strike 2",
    region: "Europe",
    major: true,
    organizer: "PGL",
    stage: "Elimination Stage",
    format: "Best of 3",
    startTime: "2025-11-20T14:00:00Z",
    endTime: "2025-11-20T21:00:00Z",
    venue: "Copenhagen, Denmark",
    streams: [
      { label: "Twitch", url: "https://twitch.tv/pgl" },
      { label: "YouTube", url: "https://youtube.com/pgl" }
    ]
  },
  {
    id: "apex-global-series",
    title: "ALGS Year 5 Split 1 Playoffs",
    game: "Apex Legends",
    region: "Global",
    major: false,
    organizer: "EA",
    stage: "Day 3",
    format: "Match Point",
    startTime: "2025-11-02T20:00:00Z",
    endTime: "2025-11-02T23:30:00Z",
    venue: "Arlington, USA",
    streams: [
      { label: "Twitch", url: "https://twitch.tv/playapex" },
      { label: "YouTube", url: "https://youtube.com/playapex" }
    ]
  },
  {
    id: "overwatch-champions-series",
    title: "OWCS Major #3",
    game: "Overwatch 2",
    region: "Global",
    major: false,
    organizer: "Blizzard",
    stage: "Top 8",
    format: "Double Elimination",
    startTime: "2025-11-03T18:00:00Z",
    endTime: "2025-11-03T22:00:00Z",
    venue: "Online",
    streams: [{ label: "Twitch", url: "https://twitch.tv/overwatchleague" }]
  },
  {
    id: "fortnite-ccs",
    title: "FNCS Global Championship Qualifier",
    game: "Fortnite",
    region: "South America",
    major: false,
    organizer: "Epic Games",
    stage: "Heat 2",
    format: "Battle Royale",
    startTime: "2025-11-01T22:00:00Z",
    endTime: "2025-11-02T02:00:00Z",
    venue: "Sao Paulo, Brazil",
    streams: [{ label: "Twitch", url: "https://twitch.tv/fortnite" }]
  },
  {
    id: "smash-summit",
    title: "Smash Summit 16",
    game: "Super Smash Bros. Melee",
    region: "North America",
    major: false,
    organizer: "Beyond the Summit",
    stage: "Finals Day",
    format: "Double Elimination",
    startTime: "2025-11-09T19:00:00Z",
    endTime: "2025-11-09T23:59:00Z",
    venue: "Los Angeles, USA",
    streams: [{ label: "Twitch", url: "https://twitch.tv/btssmash" }]
  },
  {
    id: "pubg-nations-cup",
    title: "PUBG Nations Cup 2025",
    game: "PUBG",
    region: "Asia",
    major: true,
    organizer: "Krafton",
    stage: "Championship Sunday",
    format: "Point based",
    startTime: "2025-11-16T12:00:00Z",
    endTime: "2025-11-16T17:00:00Z",
    venue: "Bangkok, Thailand",
    streams: [{ label: "YouTube", url: "https://youtube.com/pubgesports" }]
  }
];

const FILTER_STORAGE_KEY = "cooldown-gg/filters";

const elements = {
  game: document.querySelector("#filter-game"),
  region: document.querySelector("#filter-region"),
  status: document.querySelector("#filter-status"),
  major: document.querySelector("#filter-major"),
  reset: document.querySelector("#reset-filters"),
  list: document.querySelector("#event-list"),
  badge: document.querySelector("#visible-count"),
  empty: document.querySelector("#empty-state")
};

const initialState = loadFilters() ?? {
  game: "all",
  region: "all",
  status: "upcoming",
  major: false
};

const state = { ...initialState };

function init() {
  populateFilterOptions();
  applyStateToControls();
  attachListeners();
  render();
  setInterval(render, 30_000);
}

function populateFilterOptions() {
  const games = new Set();
  const regions = new Set();

  EVENTS.forEach((event) => {
    games.add(event.game);
    regions.add(event.region);
  });

  addOptions(elements.game, games);
  addOptions(elements.region, regions);
}

function addOptions(select, values) {
  const fragment = document.createDocumentFragment();
  [...values]
    .sort((a, b) => a.localeCompare(b))
    .forEach((value) => {
      const option = document.createElement("option");
      option.value = value;
      option.textContent = value;
      fragment.appendChild(option);
    });
  select.appendChild(fragment);
}

function applyStateToControls() {
  elements.game.value = state.game;
  elements.region.value = state.region;
  elements.status.value = state.status;
  elements.major.checked = state.major;
}

function attachListeners() {
  elements.game.addEventListener("change", () => updateState({ game: elements.game.value }));
  elements.region.addEventListener("change", () =>
    updateState({ region: elements.region.value })
  );
  elements.status.addEventListener("change", () =>
    updateState({ status: elements.status.value })
  );
  elements.major.addEventListener("change", () => updateState({ major: elements.major.checked }));
  elements.reset.addEventListener("click", () => {
    updateState({ game: "all", region: "all", status: "upcoming", major: false });
    applyStateToControls();
  });
}

function updateState(patch) {
  Object.assign(state, patch);
  saveFilters();
  render();
}

function saveFilters() {
  localStorage.setItem(FILTER_STORAGE_KEY, JSON.stringify(state));
}

function loadFilters() {
  try {
    const stored = localStorage.getItem(FILTER_STORAGE_KEY);
    return stored ? JSON.parse(stored) : null;
  } catch (error) {
    console.warn("Failed to parse stored filters", error);
    return null;
  }
}

function render() {
  const now = new Date();
  const filtered = EVENTS.filter((event) => matchesFilters(event, now));
  const sorted = filtered.sort((a, b) => new Date(a.startTime) - new Date(b.startTime));

  elements.list.innerHTML = "";
  elements.badge.textContent = `${sorted.length} event${sorted.length === 1 ? "" : "s"}`;
  elements.empty.hidden = sorted.length > 0;

  sorted.forEach((event) => {
    elements.list.appendChild(renderEventCard(event, now));
  });
}

function matchesFilters(event, now) {
  const status = getEventStatus(event, now);

  if (state.status !== "all" && status !== state.status) {
    return false;
  }
  if (state.game !== "all" && event.game !== state.game) {
    return false;
  }
  if (state.region !== "all" && event.region !== state.region) {
    return false;
  }
  if (state.major && !event.major) {
    return false;
  }
  return true;
}

function renderEventCard(event, now) {
  const status = getEventStatus(event, now);
  const li = document.createElement("li");
  li.className = "event-card";
  li.innerHTML = `
    <div class="event-card__top">
      <div>
        <p class="event-card__meta">${event.game} • ${event.region} • ${event.organizer}</p>
        <h3 class="event-card__title">${event.title}</h3>
      </div>
      <span class="status-pill" data-status="${status}">${statusLabel(status)}</span>
    </div>
    <div class="event-card__tags">
      <span class="tag">${event.stage}</span>
      <span class="tag">${event.format}</span>
      <span class="tag">${event.venue}</span>
      ${event.major ? '<span class="tag tag--major">Major</span>' : ""}
    </div>
    <div class="event-card__footer">
      <div>
        <div class="countdown">${countdownLabel(event, status, now)}</div>
        <div class="time">${formatDateRange(event)}</div>
      </div>
      ${renderStreams(event.streams)}
    </div>
  `;
  return li;
}

function renderStreams(streams) {
  if (!streams?.length) {
    return "";
  }
  const links = streams
    .map((stream) => `<a href="${stream.url}" target="_blank" rel="noopener">${stream.label}</a>`)
    .join("");
  return `<div class="stream-links" aria-label="Streams">${links}</div>`;
}

function getEventStatus(event, now = new Date()) {
  const start = new Date(event.startTime);
  const end = event.endTime ? new Date(event.endTime) : null;

  if (now < start) {
    return "upcoming";
  }
  if (end && now <= end) {
    return "live";
  }
  if (!end && now - start < 4 * 60 * 60 * 1000) {
    return "live"; // fallback live window of 4 hours
  }
  return "completed";
}

function statusLabel(status) {
  switch (status) {
    case "upcoming":
      return "Upcoming";
    case "live":
      return "Live";
    case "completed":
      return "Completed";
    default:
      return status;
  }
}

function countdownLabel(event, status, now) {
  const start = new Date(event.startTime);
  const end = event.endTime ? new Date(event.endTime) : null;

  if (status === "upcoming") {
    return `Starts in ${formatDuration(start - now)}`;
  }
  if (status === "live") {
    const remaining = end ? end - now : start.getTime() + 4 * 60 * 60 * 1000 - now.getTime();
    return remaining > 0
      ? `Live now • ${formatDuration(remaining)} remaining`
      : "Live now";
  }
  return `Ended ${formatDuration(now - (end ?? start))} ago`;
}

function formatDateRange(event) {
  const start = new Date(event.startTime);
  const end = event.endTime ? new Date(event.endTime) : null;
  const startFormatter = new Intl.DateTimeFormat(undefined, {
    weekday: "short",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
    timeZoneName: "short"
  });
  const endFormatter = new Intl.DateTimeFormat(undefined, {
    hour: "numeric",
    minute: "2-digit"
  });
  if (!end) {
    return startFormatter.format(start);
  }
  return `${startFormatter.format(start)} → ${endFormatter.format(end)}`;
}

function formatDuration(ms) {
  const abs = Math.max(0, Math.abs(ms));
  const totalMinutes = Math.floor(abs / (60 * 1000));
  const days = Math.floor(totalMinutes / (24 * 60));
  const hours = Math.floor((totalMinutes % (24 * 60)) / 60);
  const minutes = totalMinutes % 60;

  const parts = [];
  if (days) parts.push(`${days}d`);
  if (hours) parts.push(`${hours}h`);
  if (minutes || !parts.length) parts.push(`${minutes}m`);
  return parts.join(" ");
}

init();
