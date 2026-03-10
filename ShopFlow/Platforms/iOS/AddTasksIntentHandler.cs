using Intents;
using Foundation;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.Repositories;
using BrenoMachado.ShopFlow.Models;
using Microsoft.Maui;

namespace BrenoMachado.ShopFlow.Platforms.iOS
{
    [Register("AddTasksIntentHandler")]
    public class AddTasksIntentHandler : NSObject, IINAddTasksIntentHandling
    {
        // Must be empty constructor for generic instantiation if needed
        public AddTasksIntentHandler()
        {
        }

        public AddTasksIntentHandler(IntPtr handle) : base(handle)
        {
        }

        public void ResolveTargetTaskList(INAddTasksIntent intent, Action<INTaskListResolutionResult> completion)
        {
            // We only have one "primary" concept for Siri right now: The default shopping list.
            // If the user specified a list name, we could try to find it.
            // For now, let's keep it simple: Always resolve to "not required" (implies default) or confirm default.
            
            var targetList = intent.TargetTaskList;
            if (targetList == null)
            {
                completion(INTaskListResolutionResult.NotRequired);
                return;
            }

            FileLoggerService.LogStatic("Siri ResolveTargetTaskList invoked.");
            completion(INTaskListResolutionResult.GetSuccess(targetList));
        }

        public void ResolveTaskTitles(INAddTasksIntent intent, Action<INSpeakableStringResolutionResult[]> completion)
        {
            var taskTitles = intent.TaskTitles;
            if (taskTitles == null || taskTitles.Length == 0)
            {
                completion(new INSpeakableStringResolutionResult[] { INSpeakableStringResolutionResult.NeedsValue });
                return;
            }

            var results = new List<INSpeakableStringResolutionResult>();
            foreach (var title in taskTitles)
            {
                results.Add(INSpeakableStringResolutionResult.GetSuccess(title));
            }
            completion(results.ToArray());
        }

        public void ResolveSpatialEventTrigger(INAddTasksIntent intent, Action<INSpatialEventTriggerResolutionResult> completion)
        {
            completion(INSpatialEventTriggerResolutionResult.NotRequired);
        }

        public void ResolveTemporalEventTrigger(INAddTasksIntent intent, Action<INTemporalEventTriggerResolutionResult> completion)
        {
            completion(INTemporalEventTriggerResolutionResult.NotRequired);
        }

        public void ResolvePriority(INAddTasksIntent intent, Action<INTaskPriorityResolutionResult> completion)
        {
             completion(INTaskPriorityResolutionResult.NotRequired);
        }

        public void Confirm(INAddTasksIntent intent, Action<INAddTasksIntentResponse> completion)
        {
            // Ready to handle
            completion(new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Ready, null));
        }

        public void HandleAddTasks(INAddTasksIntent intent, Action<INAddTasksIntentResponse> completion)
        {
            FileLoggerService.LogStatic("[Siri] HandleAddTasks invoked");
            var taskTitles = intent.TaskTitles;
            if (taskTitles == null || taskTitles.Length == 0)
            {
                FileLoggerService.LogStatic("[Siri] No task titles provided.");
                completion(new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Failure, null));
                return;
            }

            try
            {
                // Resolve Dependencies
                var app = IPlatformApplication.Current;
                if (app == null) 
                {
                    FileLoggerService.LogStatic("[Siri] IPlatformApplication.Current is null. Cannot resolve dependencies.");
                    completion(new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Failure, null));
                    return;
                }

                var services = app.Services;
                var repo = services.GetService<IShoppingRepository>();
                var smartService = services.GetService<ISmartService>();
                var graphQLService = services.GetService<IGraphQLService>();

                if (repo == null || smartService == null || graphQLService == null)
                {
                     FileLoggerService.LogStatic("[Siri] Failed to resolve required services.");
                     completion(new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Failure, null));
                     return;
                }
                
                MainThread.BeginInvokeOnMainThread(async () => {
                    try 
                    {
                        FileLoggerService.LogStatic("[Siri] MainThread started. Fetching lists...");
                        var lists = await repo.GetListsAsync(); // Fetch from repo, not graphQL to support offline/local fast sync
                        var defaultList = lists.FirstOrDefault(l => l.IsDefault) ?? lists.FirstOrDefault();
                        
                        if (defaultList == null)
                        {
                            FileLoggerService.LogStatic("[Siri] No default list found.");
                            completion(new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Failure, null));
                            return;
                        }

                        var addedTasks = new List<INTask>();

                        foreach (var title in taskTitles)
                        {
                            string rawText = title.SpokenPhrase;
                            FileLoggerService.LogStatic($"[Siri] Processing item: {rawText}");
                            
                            var parsed = smartService.ParseItemInput(rawText);
                            
                            var newItem = new ShoppingItem 
                            { 
                                ListId = defaultList.Id, 
                                Name = parsed.Name, 
                                Quantity = parsed.Quantity, 
                                Unit = parsed.Unit, 
                                Category = "Siri" 
                            };
                            
                            await repo.SaveItemAsync(newItem);
                            FileLoggerService.LogStatic($"[Siri] Saved item: {newItem.Name}");
                            
                            addedTasks.Add(new INTask(
                                title: new INSpeakableString(newItem.Name),
                                status: INTaskStatus.NotCompleted,
                                taskType: INTaskType.Completable,
                                spatialEventTrigger: null,
                                temporalEventTrigger: null,
                                createdDateComponents: null,
                                modifiedDateComponents: null,
                                identifier: newItem.Id,
                                priority: INTaskPriority.NotFlagged
                            ));
                        }

                        var response = new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Success, null)
                        {
                            AddedTasks = addedTasks.ToArray()
                        };
                        FileLoggerService.LogStatic("[Siri] Returning Success response.");
                        completion(response);
                    }
                    catch(Exception ex)
                    {
                         FileLoggerService.LogStatic($"[Siri] Inner Handle Error: {ex}");
                         completion(new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Failure, null));
                    }
                });
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"[Siri] Outer Handle Error: {ex}");
                completion(new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Failure, null));
            }
        }
    }
}
