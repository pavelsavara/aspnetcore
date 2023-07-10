﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal class FormDataMappingException(FormDataMappingError error) : Exception
{
    public FormDataMappingError Error { get; } = error;
}
