/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Collections.Concurrent;
using System.Diagnostics;
using KofTwentyTwo.AppKit.Interaction;
using KofTwentyTwo.AppKit.Updates;


namespace KofTwentyTwo.AppKit.Tests;

/// <summary>Exercises genuinely asynchronous update completions while a single UI thread pumps its context.</summary>
public sealed class UpdateContextTests
{
   /// <summary>Check, confirmation and download completions return to the UI context before the next prompt.</summary>
   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Check_AsynchronousUpdate_InvokesPromptsOnOriginalContext(bool quietly)
   {
      using var context = new PumpedContext();
      var prompter = new ContextPrompter(context);
      var coordinator = new UpdateCoordinator(new ContextUpdates(context), prompter, "Sample");
      await context.Run(() => quietly ? coordinator.CheckQuietlyAsync() : coordinator.CheckInteractivelyAsync());
      Assert.Equal(1, prompter.Confirmations);
      Assert.Equal(["disk full"], prompter.Messages);
   }



   /// <summary>The UI-independent backend applies after download without requiring the caller's UI context.</summary>
   [Fact]
   public async Task Download_AsynchronousCompletion_AppliesWithoutUiContext()
   {
      using var context = new PumpedContext();
      var backend = new ContextBackend(context);
      var service = new VelopackUpdateService(() => backend);
      await context.Run(async () =>
      {
         Assert.True((await service.CheckAsync()).IsUpdateAvailable);
         Assert.Null(await service.DownloadAndApplyAsync());
      });
      Assert.True(backend.Applied);
   }



   /// <summary>A bounded single-thread pump; completions are queued so each tested await starts with an incomplete task.</summary>
   private sealed class PumpedContext : SynchronizationContext, IDisposable
   {
      private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _callbacks = new();



      /// <summary>Queues a callback for the thread running the pump.</summary>
      public override void Post(SendOrPostCallback d, object? state) => _callbacks.Add((d, state));



      /// <summary>Runs an asynchronous operation while pumping callbacks, then restores the test framework's context.</summary>
      public Task Run(Func<Task> start)
      {
         SynchronizationContext? previous = Current;
         SetSynchronizationContext(this);
         try
         {
            Task operation = start();
            long began = Stopwatch.GetTimestamp();
            while(!operation.IsCompleted)
            {
               Assert.True(Stopwatch.GetElapsedTime(began) < TimeSpan.FromSeconds(30), "Update flow did not complete while pumping its UI context.");
               if(_callbacks.TryTake(out (SendOrPostCallback Callback, object? State) callback, millisecondsTimeout: 50))
               {
                  callback.Callback(callback.State);
               }
            }
            return operation;
         }
         finally
         {
            SetSynchronizationContext(previous);
         }
      }



      /// <summary>Completes after the next pump iteration, with continuations scheduled rather than inlined.</summary>
      public Task<T> CompleteLater<T>(T value)
      {
         var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
         Post(_ => completion.SetResult(value), null);
         return completion.Task;
      }



      /// <summary>Releases the callback queue after the operation has finished.</summary>
      public void Dispose() => _callbacks.Dispose();
   }



   /// <summary>An update service whose operations complete asynchronously through the test UI pump.</summary>
   private sealed class ContextUpdates(PumpedContext context) : IUpdateService
   {
      public bool IsSupported => true;

      public string? CurrentVersion => "1.0.0";



      /// <summary>Offers a version only after the caller has yielded.</summary>
      public Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
          => context.CompleteLater(UpdateCheckResult.Available("2.0.0"));



      /// <summary>Returns a recoverable installation failure after the caller has yielded.</summary>
      public Task<string?> DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default)
          => context.CompleteLater<string?>("disk full");
   }



   /// <summary>A backend that completes through the pump but verifies installation proceeds without a UI context.</summary>
   private sealed class ContextBackend(PumpedContext context) : IUpdateBackend
   {
      public bool IsInstalled => true;

      public string? CurrentVersion => "1.0.0";

      public bool Applied { get; private set; }



      /// <summary>Checks complete after the caller yields.</summary>
      public Task<string?> CheckAsync() => context.CompleteLater<string?>("2.0.0");



      /// <summary>Downloads complete after the caller yields.</summary>
      public Task DownloadAsync(Action<int>? progress, CancellationToken cancellationToken) => context.CompleteLater(true);



      /// <summary>Rejects a UI-context dependency before recording the application.</summary>
      public void ApplyAndRestart()
      {
         Assert.Null(SynchronizationContext.Current);
         Applied = true;
      }
   }



   /// <summary>A dialog adapter that rejects calls from a worker thread or another synchronization context.</summary>
   private sealed class ContextPrompter(PumpedContext context) : IUserPrompter
   {
      public int Confirmations { get; private set; }

      public List<string> Messages { get; } = [];



      /// <summary>Checks context affinity and records the installation failure.</summary>
      public Task ShowMessageAsync(string title, string message)
      {
         Assert.Same(context, SynchronizationContext.Current);
         Messages.Add(message);
         return context.CompleteLater(true);
      }



      /// <summary>Checks context affinity and answers asynchronously, exercising the next continuation too.</summary>
      public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText)
      {
         Assert.Same(context, SynchronizationContext.Current);
         Confirmations++;
         return context.CompleteLater(true);
      }
   }
}
