// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.Migrations.Model;
using Microsoft.Data.Entity.Relational;
using Moq;
using Xunit;

namespace Microsoft.Data.Entity.Migrations.Tests.Model
{
    public class DropTableOperationTest
    {
        [Fact]
        public void Create_and_initialize_operation()
        {
            var dropTableOperation = new DropTableOperation("dbo.MyTable");

            Assert.Equal("dbo.MyTable", dropTableOperation.TableName);
            Assert.True(dropTableOperation.IsDestructiveChange);
        }

        [Fact]
        public void Dispatches_visitor()
        {
            var dropTableOperation = new DropTableOperation("dbo.MyTable");
            var mockVisitor = MigrationsTestHelpers.MockSqlGenerator();
            var builder = new Mock<SqlBatchBuilder>();
            dropTableOperation.GenerateSql(mockVisitor.Object, builder.Object);

            mockVisitor.Verify(g => g.Generate(dropTableOperation, builder.Object), Times.Once());
        }
    }
}
