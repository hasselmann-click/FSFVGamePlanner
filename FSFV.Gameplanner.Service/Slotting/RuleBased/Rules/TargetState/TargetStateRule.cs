using FSFV.Gameplanner.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.TargetState;
internal class TargetStateRule(int priority, TargetStateRuleConfigurationProvider targetStates, ILogger<TargetStateRule> logger) : AbstractSlotRule(priority)
{

    private const int ComparisonBufferMinutes = 30;

    public override void ProcessBeforeGameday(List<Pitch> pitches, List<Game> games)
    {
        base.ProcessBeforeGameday(pitches, games);
    }

    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {

        IEnumerable<Game> nextGames = games.ToList();

        foreach (var state in targetStates.RuleConfigs)
        {

            // skip the config if this filter doesn't match
            state.FilterBy.Deconstruct(out var team, out var round, out var time, out var date, out var pitchFilter);
            if (round != null && pitch.GameDay != round) continue;
            if (pitchFilter != null && pitch.Name != pitchFilter) continue;
            if (date != null && DateOnly.FromDateTime(pitch.StartTime) != date) continue;

            if (!MatchTimeInBuffer(pitch.NextStartTime, time)) continue;

            // TODO does the league filter make even sense?
            //if (league != null) 
            //{
            //    nextGames = nextGames.Where(g => g.Group.Type.Name == league);
            //}

            state.ToApply.Deconstruct(out var aTeam, out var aTime, out var aLeague);

            // Move the team to the current time and pitch
            if (aTeam != null)
            {
                nextGames = nextGames.Where(g => g.Home.Name == aTeam || g.Away.Name == aTeam);
                continue;
            }

            // Move the next league game to the current time and pitch
            if (aLeague != null)
            {
                nextGames = nextGames.Where(g => g.Group.Name == aLeague);
            }

        }

        if (!nextGames.Any())
        {
            logger.LogWarning("No games left after target state rules for pitch {pitch} at {time}", pitch.Name, pitch.NextStartTime.ToString("dd.MM.yyyy hh:mm"));
        }

        return games;
    }

    public override void Update(Pitch pitch, Game game)
    {
        base.Update(pitch, game);
    }

    public override void ProcessAfterGameday(List<Pitch> pitches)
    {
        // update the timeslot start time 
        var slots = pitches.SelectMany(p => p.Slots);
        foreach (var state in targetStates.RuleConfigs.Where(ts => ts.ToApply.Time.HasValue))
        {
            state.FilterBy.Deconstruct(out var team, out var round, out var time, out var date, out var league, out var fPitch);
            var slot = pitches
                .Where(p => fPitch == null || fPitch == p.Name)
                .Where(p => round == null || round == p.GameDay)
                .Where(p => date == null || date == DateOnly.FromDateTime(p.StartTime))
                .SelectMany(p => p.Slots)
                .Where(s => MatchTimeInBuffer(s.StartTime, time))
                .FirstOrDefault();
            if(slot == null)
            {
                logger.LogWarning("Couldn't find slot for filter {filter}", state.FilterBy.ToString());
                continue;
            }
            slot.StartTime = slot.StartTime.Date.Add(state.ToApply.Time.Value.ToTimeSpan());
        }

        base.ProcessAfterGameday(pitches);
    }

    private bool MatchTimeInBuffer(DateTime time, TimeOnly? timeFilter)
    {
        var res = timeFilter != null && (
                    time.AddMinutes(-ComparisonBufferMinutes).CompareTo(timeFilter) >= 0
                    && time.AddMinutes(ComparisonBufferMinutes).CompareTo(timeFilter) < 0);
        logger.LogDebug("MatchNextStartTime: {match}", res);
        return res;
    }

}
