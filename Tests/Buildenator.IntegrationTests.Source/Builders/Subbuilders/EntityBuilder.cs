﻿using Buildenator.Abstraction;
using Buildenator.IntegrationTests.SharedEntities;

namespace Buildenator.IntegrationTests.Source.Builders.SubBuilders;

[MakeBuilder(typeof(Entity), generateDefaultBuildMethod: false)]
public partial class EntityBuilder
{
}