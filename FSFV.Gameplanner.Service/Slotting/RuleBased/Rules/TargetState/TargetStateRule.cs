using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Common.Dto;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.TargetState;
internal class TargetStateRule(int priority, TargetStateRuleConfigurationProvider targetStates, ILogger<TargetStateRule> logger) : AbstractSlotRule(priority)
{

    // TODO extract to interface
    private bool Validate()
    {
        if (targetStates.RuleConfigs is null)
        {
            logger.LogDebug("No target state rules given. Add some via a target.*.csv file");
            return false;
        }

        if (targetStates.GroupTypeConfigs is null)
        {
            throw new InvalidOperationException("Can not use target state rules without group type configs");
        }

        return true;
    }

    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        if (!Validate()) return games;

        IEnumerable<Game> nextGames = games.ToList();
        foreach (var state in targetStates.RuleConfigs)
        {
            if (!ShouldApplyToday(pitch, state)) continue;

            var minDurationMinutes = GetTimeBufferMinutes(targetStates.GroupTypeConfigs, state);
            bool shouldApply = ShouldApplyNow(pitch, state, minDurationMinutes);
            logger.LogTrace("Apply rule {rule}: {apply}", state.Filter.ToString(), shouldApply);

            (var aTeam, var aTime, var aLeague) = state.Applicator;
            IEnumerable<Game> appliedRuleGames = nextGames;
            // Move the team to the current time and pitch
            if (!string.IsNullOrEmpty(aTeam))
            {
                // if has game and shouldApply: return the game
                // if has game and not shouldApply: return everything except the game
                // if not has game and shouldApply: do nothing;
                // if not has game and not shouldApply: do nothing
                appliedRuleGames = appliedRuleGames.Where(g => (HasTeam(g, aTeam) && shouldApply) || (!HasTeam(g, aTeam) && !shouldApply));
            }

            // Move the next league game to the current time and pitch
            if (!string.IsNullOrEmpty(aLeague))
            {
                appliedRuleGames = appliedRuleGames.Where(g => (HasLeague(g, aLeague) && shouldApply) || (!HasLeague(g, aLeague) && !shouldApply));
            }

            // Only filter available games if any viable options left
            if (appliedRuleGames.Any())
            {
                nextGames = appliedRuleGames;
            }

            logger.LogTrace("Possible games after rule {rule}: {games}", state.Filter, string.Join(",", nextGames.Select(g => $"[{g.Home.Name}|{g.Away.Name}|{g.Group.Type.Name}]")));
        }


        if (!nextGames.Any())
        {
            logger.LogWarning("No games left after target state rules for pitch {pitch} at {time}", pitch.Name, pitch.NextStartTime.ToString("dd.MM.yyyy hh:mm"));
            return games;
        }

        return nextGames;

    }

    private int GetTimeBufferMinutes(Dictionary<string, GroupTypeDto> groupTypeConfigs, TargetStateRuleConfiguration state)
    {
        (var _, var aTime, var aLeague) = state.Applicator;
        var minDurationMinutes = 0;
        if (aTime == null || string.IsNullOrEmpty(aLeague))
        {
            return minDurationMinutes;
        }

        if (!groupTypeConfigs.TryGetValue(aLeague, out var aLeagueConfig))
        {
            logger.LogError("Missing league config for league {league}. Can not set starting time without minimum duration", aLeague);
            return minDurationMinutes;
        }

        return aLeagueConfig.MinDurationMinutes;
    }

    private static bool HasTeam(Game game, string aTeam)
    {
        return (game.Home.Name == aTeam || game.Away.Name == aTeam);
    }

    private static bool HasLeague(Game game, string aLeague)
    {
        return game.Group.Type.Name == aLeague;
    }

    private static bool ShouldApplyToday(Pitch pitch, TargetStateRuleConfiguration state)
    {
        (var round, var _, var date, var _) = state.Filter;
        var shouldApply = (round == null || pitch.GameDay == round)
            && (date == null || pitch.Date == date);
        return shouldApply;
    }

    private static bool ShouldApplyNow(Pitch pitch, TargetStateRuleConfiguration state, int bufferMinutes = 0)
    {
        (var _, var time, var _, var pitchFilter) = state.Filter;

        // target rules have to either apply directly or prevent games from being scheduled prematurely
        var shouldApply =
            (string.IsNullOrEmpty(pitchFilter) || pitch.Name == pitchFilter)
            && (time == null ||
                // next start time (+ buffer) is after the time filter
                pitch.NextStartTime.AddMinutes(bufferMinutes).CompareTo(time) >= 0);
        return shouldApply;
    }

    public override void Update(Pitch pitch, Game game)
    {
        base.Update(pitch, game);
    }

    public override void ProcessAfterGameday(List<Pitch> pitches)
    {
        if (!Validate()) return;

        // update the timeslot start time 
        foreach (var state in targetStates.RuleConfigs.Where(ts => ts.Applicator.Time.HasValue))
        {

            if (!ShouldApplyToday(pitches.First(), state))
            {
                continue;
            }

            (var round, var time, var date, var fPitch) = state.Filter;
            (var _, var aTime, var _) = state.Applicator;

            var bufferMinutes = GetTimeBufferMinutes(targetStates.GroupTypeConfigs, state);
            var slots = pitches
                .Where(p => fPitch == null || fPitch == p.Name)
                .Where(p => round == null || round == p.GameDay)
                .Where(p => date == null || date == p.Date)
                .SelectMany(p => p.Slots)
                .Where(s => s.StartTime.AddMinutes(bufferMinutes).CompareTo(time?.ToTimeSpan()) >= 0)
                .ToList()
                ;
            if (slots.Count == 0)
            {
                logger.LogWarning("Couldn't find slots for filter {filter}", state.Filter.ToString());
                continue;
            }

            // update start time with the given rule time and update the subsequent slots
            var endTime = slots.Last().EndTime;
            var first = slots.First();
            first.StartTime = first.StartTime.Add(aTime!.Value.ToTimeSpan());
            first.EndTime = first.StartTime.AddMinutes(first.Game.Group.Type.MinDurationMinutes);

            int parallelGames = 1;
            int j = 0;
            for (int i = 1; i < slots.Count; ++i)
            {
                // handle parallel games
                if (slots[i].Game.Group.Type.ParallelGamesPerPitch < parallelGames)
                {
                    ++parallelGames;
                    slots[i].StartTime = slots[i - 1].StartTime;
                    slots[i].EndTime = slots[i - 1].EndTime;
                    continue;
                }

                // remember first slot which doesn't belong to the league, which is directly affected by the target state rule
                if (j == 0 && slots[i - 1].Game.Group.Type != slots[i].Game.Group.Type)
                {
                    j = i;
                }

                parallelGames = 1;
                slots[i].StartTime = slots[i - 1].EndTime;
                slots[i].EndTime = slots[i].StartTime.AddMinutes(slots[i].Game.Group.Type.MinDurationMinutes);
            }

            if (j == 0 || j == (slots.Count - 1)) continue;

            // TODO refactor: This taken from the initial "BuildSlotTimes" and the Pitch.TimeLeft dto
            // Add break between placeholder league and follow up slots
            var numberOfBreaks = slots.Count - j; // + 1 (index) - 1 (no break after last slot)
            var timeLeft = endTime
                - slots[j].StartTime
                - slots[j..]
                    .Select(s => s.Game)
                    .Select(g => g.MinDuration.Divide(g.Group.Type.ParallelGamesPerPitch))
                    .Aggregate(TimeSpan.Zero, (d1, d2) => d1.Add(d2));
            var additionalBreak = numberOfBreaks > 0 ? timeLeft.Divide(numberOfBreaks) : TimeSpan.Zero;
            if (additionalBreak < TimeSpan.Zero)
                additionalBreak = TimeSpan.Zero;
            else
                // round to nearest 5
                additionalBreak = TimeSpan.FromMinutes(
                    Math.Floor(additionalBreak.TotalMinutes / 5.0) * 5);

            slots[(j + 1)..].ForEach(s => s.StartTime = s.StartTime.Add(additionalBreak));
        }

        base.ProcessAfterGameday(pitches);
    }

}
