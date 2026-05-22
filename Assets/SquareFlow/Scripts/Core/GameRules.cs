using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareFlow.Core
{
    public sealed class GameRules
    {
        private readonly GameState state;
        private readonly BoardLayout layout;

        public GameRules(GameState state, BoardLayout layout)
        {
            this.state = state;
            this.layout = layout;
        }

        public bool FireFromColumn(int columnIndex)
        {
            if (state.Result != GameResult.None) return false;
            if (state.ActiveOrbiters.Count >= SquareFlowConstants.MaxActiveOrbiters)
            {
                RecordEvent(new GameEvent(GameEventType.Blocked));
                return false;
            }
            if (columnIndex < 0 || columnIndex >= state.ShooterColumns.Length) return false;
            if (state.ShooterColumns[columnIndex].Count == 0) return false;

            Shooter shooter = state.ShooterColumns[columnIndex][0];
            state.ShooterColumns[columnIndex].RemoveAt(0);
            RevealFrontShooter(columnIndex);
            FireShooter(shooter);
            return true;
        }

        public bool FireFromWaiting(int index)
        {
            if (state.Result != GameResult.None) return false;
            if (state.ActiveOrbiters.Count >= SquareFlowConstants.MaxActiveOrbiters)
            {
                RecordEvent(new GameEvent(GameEventType.Blocked));
                return false;
            }
            if (index < 0 || index >= state.WaitingQueue.Count) return false;

            Shooter shooter = state.WaitingQueue[index];
            state.WaitingQueue.RemoveAt(index);
            FireShooter(shooter);
            return true;
        }

        public List<GameEvent> Advance(float deltaSeconds)
        {
            List<GameEvent> events = new List<GameEvent>();
            if (state.Result != GameResult.None) return events;

            for (int i = state.ActiveOrbiters.Count - 1; i >= 0; i--)
            {
                ActiveOrbiter orbiter = state.ActiveOrbiters[i];
                float oldDistance = orbiter.Distance;
                orbiter.Distance += SquareFlowConstants.Speed * deltaSeconds;

                for (int p = 0; p < layout.FirePoints.Count; p++)
                {
                    FirePoint point = layout.FirePoints[p];
                    bool crossedPoint = point.Distance > oldDistance && point.Distance <= orbiter.Distance;
                    bool crossedStartPoint = Mathf.Approximately(point.Distance, 0f)
                        && Mathf.Approximately(oldDistance, 0f)
                        && orbiter.Distance > oldDistance;
                    if ((!crossedPoint && !crossedStartPoint) || orbiter.Ammo <= 0) continue;

                    TargetHit? hit = TargetingSystem.GetTarget(state.Grid, state.Shape, point, orbiter.Color, orbiter.Wild);
                    if (!hit.HasValue) continue;
                    ApplyHit(orbiter, point, hit.Value, events);
                }

                if (orbiter.Distance >= layout.Perimeter || orbiter.Ammo <= 0)
                    RemoveOrbiterAt(i, orbiter, events);
            }

            int eventCountBeforeEndConditions = state.Events.Count;
            CheckEndConditions();
            AppendEventsSince(events, eventCountBeforeEndConditions);
            return events;
        }

        public int DetonateBomb(int row, int col)
        {
            int cleared = 0;
            for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
            {
                int nr = row + dr;
                int nc = col + dc;
                if (!state.Shape.IsActive(nr, nc) || !state.Grid[nr, nc].IsOccupied) continue;
                state.Grid[nr, nc] = BoardCell.Empty;
                cleared++;
            }
            return cleared;
        }

        public void CheckEndConditions()
        {
            if (state.Result != GameResult.None) return;

            if (!state.AnyBlocksRemaining())
            {
                SetResult(GameResult.Won);
                return;
            }

            if (state.WaitingQueue.Count >= SquareFlowConstants.WaitQueueLimit)
            {
                SetResult(GameResult.LostWait);
                return;
            }

            if (!state.HasAvailableShooters())
                SetResult(GameResult.LostOutOfShooters);
        }

        public void UpdateCombo(float deltaSeconds)
        {
            if (state.ComboTimer <= 0f) return;
            state.ComboTimer -= deltaSeconds;
            if (state.ComboTimer <= 0f) state.Combo = 1f;
        }

        private void FireShooter(Shooter shooter)
        {
            state.Moves++;
            state.ActiveOrbiters.Add(new ActiveOrbiter(shooter));
            RecordEvent(new GameEvent(GameEventType.Fired, orbiterId: shooter.Id));
        }

        private void RevealFrontShooter(int columnIndex)
        {
            if (state.ShooterColumns[columnIndex].Count == 0) return;
            Shooter front = state.ShooterColumns[columnIndex][0];
            state.ShooterColumns[columnIndex][0] = front.Revealed();
        }

        private void RemoveOrbiterAt(int index, ActiveOrbiter orbiter, List<GameEvent> events)
        {
            state.ActiveOrbiters.RemoveAt(index);
            if (orbiter.Ammo > 0 && state.WaitingQueue.Count < SquareFlowConstants.WaitQueueLimit)
            {
                state.WaitingQueue.Add(new Shooter(Guid.NewGuid().ToString("N"), orbiter.Color, orbiter.Ammo, orbiter.Wild));
                GameEvent queued = new GameEvent(GameEventType.OrbiterQueued, orbiterId: orbiter.Id);
                RecordEvent(queued);
                events.Add(queued);
            }
            GameEvent removed = new GameEvent(GameEventType.OrbiterRemoved, orbiterId: orbiter.Id);
            RecordEvent(removed);
            events.Add(removed);
        }

        private void ApplyHit(ActiveOrbiter orbiter, FirePoint point, TargetHit hit, List<GameEvent> events)
        {
            BoardCell cell = state.Grid[hit.Row, hit.Col];
            orbiter.Ammo--;

            if (hit.Special == TargetSpecial.Bomb)
            {
                int cleared = DetonateBomb(hit.Row, hit.Col);
                int score = AddScore(150 + cleared * 50);
                AddFrameEvent(events, HitEvent(GameEventType.BombDetonated, point, hit, orbiter.Id, score));
                return;
            }

            int hp = cell.Hp - 1;
            if (hp <= 0)
            {
                state.Grid[hit.Row, hit.Col] = BoardCell.Empty;
                int basePoints = Mathf.FloorToInt(100f * state.Level * (orbiter.Wild ? 1.5f : 1f));
                int score = AddScore(basePoints);
                AddFrameEvent(events, HitEvent(GameEventType.BlockDestroyed, point, hit, orbiter.Id, score));
            }
            else
            {
                state.Grid[hit.Row, hit.Col] = cell.WithHp(hp);
                AddFrameEvent(events, HitEvent(GameEventType.BlockDamaged, point, hit, orbiter.Id, 0));
            }
        }

        private static GameEvent HitEvent(GameEventType type, FirePoint point, TargetHit hit, string orbiterId, int score)
        {
            return new GameEvent(
                type,
                hit.Row,
                hit.Col,
                orbiterId,
                score,
                point.Side,
                point.Row,
                point.Col);
        }

        private int AddScore(int basePoints)
        {
            float multiplier = state.Combo >= 2f ? state.Combo : 1f;
            int score = Mathf.FloorToInt(basePoints * multiplier);
            state.Score += score;
            state.Combo = Mathf.Min(state.Combo + 0.5f, 7f);
            state.ComboTimer = SquareFlowConstants.ComboResetSeconds;
            return score;
        }

        private void SetResult(GameResult result)
        {
            if (state.Result != GameResult.None) return;
            state.Result = result;
            RecordEvent(new GameEvent(GameEventType.ResultChanged, score: (int)result));
        }

        private void AddFrameEvent(List<GameEvent> events, GameEvent gameEvent)
        {
            RecordEvent(gameEvent);
            events.Add(gameEvent);
        }

        private void RecordEvent(GameEvent gameEvent)
        {
            state.Events.Add(gameEvent);
        }

        private void AppendEventsSince(List<GameEvent> events, int startIndex)
        {
            for (int i = startIndex; i < state.Events.Count; i++)
                events.Add(state.Events[i]);
        }
    }
}
