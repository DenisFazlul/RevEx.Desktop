using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using RevEx.Desktop;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views;
using RevEx.Desktop.Views.Categories;
using RevEx.Desktop.Views.Connectors;
using RevEx.Desktop.Views.Contents;
using RevEx.Desktop.Views.Tags;
using RevEx.Domain.Contracts.Category;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Contracts.ContentHistory;
using RevEx.Domain.Contracts.Tag;

AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
var now=DateTimeOffset.Parse("2026-10-29T10:00:00+03:00");
var history=new ContentHistoryEventDto(1,1,now,"Changed","author");
var historyViewModel=new ContentHistoryEventViewModel(history);
if(historyViewModel.OccurredAtText!=now.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")) throw new Exception("Local history time changed");
Window[] windows = [new MainWindow(),
    new CategorySelectionWindow([new CategoryDto(1,"Test",now,now)], null),
    new TagSelectionWindow([new TagDto(1,"Tag",1,"Group",now,now)]),
    new ConnectorSelectionWindow([new ConnectorDescriptor(Guid.NewGuid(),"Test",ConnectorProduct.Mock,"1",new Uri("http://127.0.0.1:1"),1,null)]),
    new ContentHistoryDetailsWindow(history),
    new Window { Content=new ContentDetailsView() }];
foreach(var window in windows) { window.Show(); Dispatcher.UIThread.RunJobs(); window.Close(); }
Console.WriteLine("PASS: Desktop initialization, resources, shared DTO templates and local history time (headless).");
