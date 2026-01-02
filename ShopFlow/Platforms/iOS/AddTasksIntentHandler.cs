using Intents;
using Foundation;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.Repositories;
using BrenoMachado.ShopFlow.Models;
using Microsoft.Maui;

namespace BrenoMachado.ShopFlow.Platforms.iOS
{
    public class AddTasksIntentHandler : INAddTasksIntentHandling
    {
        // Must be empty constructor for generic instantiation if needed, but we might pass dependencies manually if we use DI
        // Ideally we resolve dependencies via MauiApp.Current.Services
        public AddTasksIntentHandler()
        {
        }

        public override void ResolveTargetTaskList(INAddTasksIntent intent, Action<INTaskListResolutionResult> completion)
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

            // If user said "Add to Groceries list", we can validate if "Groceries" exists.
            // Accessing dependencies from background/handler context:
            var services = IPlatformApplication.Current.Services;
            // Note: Accessing DB from here might be risky if Main Thread is required or context is weird, 
            // but In-App handling usually runs in app process.
            
            completion(INTaskListResolutionResult.Success(targetList));
        }

        public override void ResolveTaskTitles(INAddTasksIntent intent, Action<INSpeakableStringResolutionResult[]> completion)
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
                results.Add(INSpeakableStringResolutionResult.Success(title));
            }
            completion(results.ToArray());
        }

        public override void Confirm(INAddTasksIntent intent, Action<INAddTasksIntentResponse> completion)
        {
            // Ready to handle
            completion(new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Ready, null));
        }

        public override async void Handle(INAddTasksIntent intent, Action<INAddTasksIntentResponse> completion)
        {
            var taskTitles = intent.TaskTitles;
            if (taskTitles == null || taskTitles.Length == 0)
            {
                completion(new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Failure, null));
                return;
            }

            try
            {
                // Resolve Dependencies
                var services = IPlatformApplication.Current.Services;
                var repo = services.GetService<IShoppingRepository>();
                var smartService = services.GetService<ISmartService>();
                var graphQLService = services.GetService<IGraphQLService>();
                
                // Get Default List
                var lists = await graphQLService.GetListsAsync();
                var defaultList = lists.FirstOrDefault(l => l.IsDefault == true) ?? lists.FirstOrDefault();
                
                if (defaultList == null)
                {
                    // Fallback create local if totally empty?
                     // Or fail.
                     completion(new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Failure, null));
                     return;
                }

                var addedTasks = new List<INTask>();

                foreach (var title in taskTitles)
                {
                    string rawText = title.SpokenPhrase;
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
                    
                    addedTasks.Add(new INTask(new INSpeakableString(newItem.Name), INTaskStatus.NotCompleted, null, INTaskType.Completable, null, null, null, null, null));
                }

                // Success
                var response = new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Success, null);
                response.AddedTasks = addedTasks.ToArray();
                completion(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Siri Handle Error: {ex}");
                completion(new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Failure, null));
            }
        }
    }
}
