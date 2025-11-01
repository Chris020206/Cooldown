# Cooldown.GG

A lightweight static prototype for tracking upcoming esports events across multiple titles.

> **Note**
> This repository is a recreation of the Cooldown.GG prototype that was previously shared via
> https://chatgpt.com/s/cd_690662722b3081919dffabcd3ba2ca46 so that it can be restored to GitHub
> after the original copy was removed.

## Getting started

Open `index.html` in any modern browser. The experience is completely client side and requires no build step.

## Features

- Curated list of major esports events with game, region, stage, and format metadata.
- Real-time countdowns that automatically refresh every 30 seconds.
- Filter controls for game, region, status (upcoming/live/completed), and major-only toggle stored in local storage.
- Quick links out to official broadcast streams.
- Responsive layout tailored for desktop and mobile viewports.

## Development notes

All styling lives in `styles.css`, while the scheduling logic is provided by `app.js`. To expand the schedule, append additional objects to the `EVENTS` array following the existing schema.
