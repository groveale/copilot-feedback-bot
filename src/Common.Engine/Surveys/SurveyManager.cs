﻿using Entities.DB.Entities;
using Entities.DB.Entities.AuditLog;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Common.Engine.Surveys;

public class SurveyManager
{
    private readonly ISurveyManagerDataLoader _dataLoader;
    private readonly ISurveyProcessor _surveyProcessor;
    private readonly ILogger _logger;

    public SurveyManager(ISurveyManagerDataLoader dataLoader, ISurveyProcessor surveyProcessor, ILogger<SurveyManager> logger)
    {
        _dataLoader = dataLoader;
        _surveyProcessor = surveyProcessor;
        _logger = logger;
    }

    public async Task<int> FindAndProcessNewSurveyEventsAllUsers()
    {
        _logger.LogInformation(nameof(FindAndProcessNewSurveyEventsAllUsers));
        var users = await _dataLoader.GetUsersWithActivity();
        var result = new SurveyPendingActivities();
        foreach (var user in users)
        {
            var unsurveyedActivities = await FindNewSurveyEvents(user);
            _logger.LogInformation($"Found {unsurveyedActivities.Count} unsurveyed activities for user {user.UserPrincipalName}");
            result.Add(unsurveyedActivities);
        }
        _logger.LogInformation($"Found {result.Count} unsurveyed activities for all users");
        await _surveyProcessor.ProcessSurveyRequest(result);
        return result.FileEvents.Count + result.MeetingEvents.Count;
    }

    public async Task<SurveyPendingActivities> FindNewSurveyEvents(User user)
    {
        var lastUserSurveyDate = await _dataLoader.GetLastUserSurveyDate(user);
        var unsurveyedActivities = await _dataLoader.GetUnsurveyedActivities(user, lastUserSurveyDate);

        var result = new SurveyPendingActivities();
        foreach (var item in unsurveyedActivities)
        {
            if (item is CopilotEventMetadataFile)
            {
                result.FileEvents.Add((CopilotEventMetadataFile)item);
            }
            else if (item is CopilotEventMetadataMeeting)
            {
                result.MeetingEvents.Add((CopilotEventMetadataMeeting)item);
            }
        }
        return result;
    }

    public ISurveyManagerDataLoader Loader => _dataLoader;
}

public class SurveyPendingActivities
{
    public List<CopilotEventMetadataFile> FileEvents { get; set; } = new();
    public List<CopilotEventMetadataMeeting> MeetingEvents { get; set; } = new();

    public void Add(SurveyPendingActivities unsurveyedActivities)
    {
        FileEvents.AddRange(unsurveyedActivities.FileEvents);
        MeetingEvents.AddRange(unsurveyedActivities.MeetingEvents);
    }

    public BaseCopilotEvent? GetNext()
    {
        if (MeetingEvents.Count > 0)
        {
            return MeetingEvents.OrderBy(e => e.Event.TimeStamp).First();
        }
        if (FileEvents.Count > 0)
        {
            return FileEvents.OrderBy(e => e.Event.TimeStamp).First();
        }
        return null;
    }

    [JsonIgnore]
    public int Count => FileEvents.Count + MeetingEvents.Count;

    [JsonIgnore]
    public bool IsEmpty => Count == 0;
}

