# Decision: Create docs/examples.md Usage Guide

**Author:** Dawes (DevRel)
**Date:** 2025-07-23
**Status:** Proposed

## Context

The SDK had no cookbook-style documentation showing developers how to use each feature. The README covers high-level architecture but lacks copy-paste-ready examples.

## Decision

Created `docs/examples.md` — a 16-section comprehensive usage guide covering the entire public API surface: Builder, Agents, Coordinator, Events, Hooks, Cost Tracking, Sessions, Config, Skills, Sharing, Storage, Platform, Multi-Squad, Casting, and Advanced Patterns. Every code example references verified types and method signatures.

## Rationale

- Reduces onboarding friction for new adopters
- Serves as living documentation alongside the README
- Every example was validated against actual source code to prevent drift
