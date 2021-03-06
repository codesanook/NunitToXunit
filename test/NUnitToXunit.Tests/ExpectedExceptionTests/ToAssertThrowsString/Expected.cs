﻿// Copyright (c) 2018 Jetabroad Pty Limited. All Rights Reserved.
// Licensed under the MIT license. See the LICENSE.md file in the project root for license information.

using Xunit;

namespace NUnitToXUnit.Testing
{
    public class UnitTest
    {
        [Xunit.Fact]
        public void TestDivisionByZero()
        {
            var exception = Assert.Throws<Exception>(() =>
            {
                var a = 1 / 0;
            });
            Assert.Equal("Division by constant zero", exception.Message);
        }
    }
}
