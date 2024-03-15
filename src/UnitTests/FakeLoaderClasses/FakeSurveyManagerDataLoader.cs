﻿using Common.Engine.Surveys;
using Entities.DB.Entities;
using Entities.DB.Entities.AuditLog;

namespace UnitTests.FakeLoaderClasses;

internal class FakeSurveyProcessor : ISurveyProcessor
{
    public Task ProcessSurveyRequest(SurveyPendingActivities activities)
    {
        Console.WriteLine("Fake processing survey request");
        return Task.CompletedTask;
    }
}

internal class FakeSurveyManagerDataLoader : ISurveyManagerDataLoader
{
    private readonly TestsConfig _testsConfig;

    static Guid ID_FILE = Guid.NewGuid();
    static Guid ID_MEETING = Guid.NewGuid();

    private List<Guid> _ids = new List<Guid>();

    public FakeSurveyManagerDataLoader(TestsConfig testsConfig)
    {
        _testsConfig = testsConfig;
    }

    public Task<DateTime?> GetLastUserSurveyDate(User user)
    {
        return Task.FromResult<DateTime?>(null);
    }

    public Task<List<BaseCopilotEvent>> GetUnsurveyedActivities(User user, DateTime? from)
    {
        var list = new List<BaseCopilotEvent>();

        if (!_ids.Contains(ID_FILE))
        {
            list.Add(new CopilotEventMetadataFile
            {
                AppHost = "unit",
                FileName = new SPEventFileName { Name = _testsConfig.TeamSitesFileName },
                FileExtension = new SPEventFileExtension { Name = _testsConfig.TeamSiteFileExtension },
                Url = new Entities.DB.Entities.SP.Url { FullUrl = _testsConfig.TeamSiteFileUrl },
                Event = new CommonAuditEvent
                {
                    Id = ID_FILE,
                    User = user
                },
            });
        }

        if (!_ids.Contains(ID_MEETING))
        {
            list.Add(new CopilotEventMetadataMeeting
            {
                AppHost = "unit",
                Event = new CommonAuditEvent
                {
                    Id = ID_MEETING,
                    User = user
                },
            });
        }
        return Task.FromResult(list);
    }

    public Task<User> GetUser(string upn)
    {
        return Task.FromResult(new User { UserPrincipalName = upn });
    }

    public Task LogSurveyRequested(CommonAuditEvent @event)
    {
        _ids.Add(@event.Id);
        return Task.CompletedTask;
    }

    public Task<List<User>> GetUsersWithActivity()
    {
        return Task.FromResult(new List<User> { new User { UserPrincipalName = "testupn" } });
    }

    public Task<int> UpdateSurveyResultWithInitialScore(CommonAuditEvent @event, int score)
    {
        Console.WriteLine($"Fake user updated survey result for {@event.Id} with score {score}");
        return Task.FromResult(1);
    }

    public Task StopBotheringUser(string upn, DateTime until)
    {
        Console.WriteLine($"Fake user requested stopping bothering until {until}");
        return Task.CompletedTask;
    }

    public Task<int> LogDisconnectedSurveyResult(int scoreGiven, string userUpn)
    {
        Console.WriteLine($"Fake user logged disconnected survey result with score {scoreGiven}");
        return Task.FromResult(1);
    }

    public Task LogSurveyFollowUp(int surveyIdUpdatedOrCreated, SurveyFollowUpModel surveyFollowUp)
    {
        throw new NotImplementedException();
    }

    Task<int> ISurveyManagerDataLoader.LogSurveyRequested(CommonAuditEvent @event)
    {
        throw new NotImplementedException();
    }
}
