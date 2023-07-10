// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering;

// When Blazor is deployed with multi-threaded runtime, WebAssemblyDispatcher will help to dispatch all JS interop calls to the main thread.
// This is necessary because all JS objects have thread affinity. They are only available on the thread (WebWorker) which created them.
// Also DOM is only available on the main (browser) thread.
// The calls to InvokeAsync methods are dispatched synchronously and the returned Task is only resolved after the main thread finished the callback.
internal sealed class WebAssemblyDispatcher : Dispatcher
{
    public static readonly Dispatcher Instance = new WebAssemblyDispatcher();
    private readonly SynchronizationContext? _context;

    private WebAssemblyDispatcher()
    {
        // capture the JSSynchronizationContext from the main thread.
        _context = SynchronizationContext.Current;
    }

    public override bool CheckAccess() => SynchronizationContext.Current == _context || _context == null;

    public override Task InvokeAsync(Action workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        if (CheckAccess())
        {
            workItem();
            return Task.CompletedTask;
        }

        _context!.InvokeAsync(workItem);
        return Task.CompletedTask;
    }

    public override Task InvokeAsync(Func<Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        if (CheckAccess())
        {
            return workItem();
        }

        return _context!.InvokeAsync(workItem);
    }

    public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        if (CheckAccess())
        {
            return Task.FromResult(workItem());
        }

        return _context!.InvokeAsync(static (workItem) => Task.FromResult(workItem()), workItem);
    }

    public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        if (CheckAccess())
        {
            return workItem();
        }

        return _context!.InvokeAsync(workItem);
    }
}

internal static class SynchronizationContextExtension
{
    public static void InvokeAsync(this SynchronizationContext self, Action body)
    {
        Exception? exc = default;
        self.Send((_) =>
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                exc = ex;
            }
        }, null);
        if (exc != null)
        {
            throw exc;
        }
    }

    public static TRes InvokeAsync<TRes>(this SynchronizationContext self, Func<TRes> body)
    {
        TRes? value = default;
        Exception? exc = default;
        self.Send((_) =>
        {
            try
            {
                value = body();
            }
            catch (Exception ex)
            {
                exc = ex;
            }
        }, null);
        if (exc != null)
        {
            throw exc;
        }
        return value!;
    }

    public static TRes InvokeAsync<T1, TRes>(this SynchronizationContext self, Func<T1, TRes> body, T1 p1)
    {
        TRes? value = default;
        Exception? exc = default;
        self.Send((_) =>
        {
            try
            {
                value = body(p1);
            }
            catch (Exception ex)
            {
                exc = ex;
            }
        }, null);
        if (exc != null)
        {
            throw exc;
        }
        return value!;
    }
}
