// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static class SR
{
    public const string ArgumentOutOfRange_Month = nameof(ArgumentOutOfRange_Month);
    public const string Arg_HTCapacityOverflow = nameof(ArgumentOutOfRange_Month);
    public const string Arg_RankMultiDimNotSupported = nameof(ArgumentOutOfRange_Month);
    public const string Argument_IncompatibleArrayType = nameof(ArgumentOutOfRange_Month);
    public const string Arg_ArrayPlusOffTooSmall = nameof(ArgumentOutOfRange_Month);
    public const string ArgumentOutOfRange_NeedNonNegNum = nameof(ArgumentOutOfRange_Month);
    public const string Arg_NonZeroLowerBound = nameof(ArgumentOutOfRange_Month);
    public const string InvalidOperationOnDefaultArray = nameof(ArgumentOutOfRange_Month);
    public const string CapacityMustEqualCountOnMove = nameof(ArgumentOutOfRange_Month);
    public const string CapacityMustBeGreaterThanOrEqualToCount = nameof(ArgumentOutOfRange_Month);
    public const string CannotFindOldValue = nameof(ArgumentOutOfRange_Month);
    public const string ArrayInitializedStateNotEqual = nameof(ArgumentOutOfRange_Month);
    public const string ArrayLengthsNotEqual = nameof(ArgumentOutOfRange_Month);
    public const string CollectionModifiedDuringEnumeration = nameof(ArgumentOutOfRange_Month);
    public const string DuplicateKey = nameof(ArgumentOutOfRange_Month);
    public const string InvalidEmptyOperation = nameof(ArgumentOutOfRange_Month);
    public const string Arg_KeyNotFoundWithKey = nameof(ArgumentOutOfRange_Month);

    public static string Format(string message, params object[] args) => string.Format(message, args);
}
