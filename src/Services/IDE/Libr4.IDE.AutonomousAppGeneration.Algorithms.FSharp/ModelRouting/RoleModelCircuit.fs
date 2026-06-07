module Libr4.IDE.AutonomousAppGeneration.Algorithms.ModelRouting.RoleModelCircuit

open System

/// Closed=0, Open=1, HalfOpen=2
type CircuitStateDto =
    { Current: int
      Failures: int
      OpenedAtUtc: DateTime option }

let closed = 0
let openState = 1
let halfOpen = 2

let createClosed () =
    { Current = closed; Failures = 0; OpenedAtUtc = None }

let isOpen (state: CircuitStateDto) (nowUtc: DateTime) (openSeconds: int) =
    if state.Current <> openState then
        false
    else
        match state.OpenedAtUtc with
        | None -> true
        | Some opened ->
            if nowUtc - opened >= TimeSpan.FromSeconds(float openSeconds) then
                false
            else
                true

let shouldTransitionToHalfOpen (state: CircuitStateDto) (nowUtc: DateTime) (openSeconds: int) =
    state.Current = openState
    && state.OpenedAtUtc.IsSome
    && nowUtc - state.OpenedAtUtc.Value >= TimeSpan.FromSeconds(float openSeconds)

let onSuccess (state: CircuitStateDto) =
    if state.Current = halfOpen || state.Current = openState || state.Failures > 0 then
        createClosed ()
    else
        state

let onFailure (state: CircuitStateDto) (threshold: int) (nowUtc: DateTime) =
    if state.Current = openState then
        state
    else
        let failures = state.Failures + 1

        if failures >= threshold then
            { Current = openState; Failures = failures; OpenedAtUtc = Some nowUtc }
        else
            { state with Failures = failures }

let toHalfOpen (state: CircuitStateDto) =
    { state with Current = halfOpen }

let buildKey (role: string) (model: string) =
    let normalizedRole =
        let lower = role.Trim().ToLowerInvariant()

        if
            lower = "explore"
            || lower = "implementer"
            || lower = "verify"
            || lower = "repair"
            || lower = "computer"
        then
            lower
        else
            "implementer"

    $"{normalizedRole}:{model.Trim()}"
