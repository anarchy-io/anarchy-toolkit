using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Entity.Event;
using Anarchy.Toolkit.Core.Events.Service.Event.Domain;
using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;
using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Handler;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using System.Reflection;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Core.Service.Event.Domain.Handler;

using Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;

public abstract class DomainEventHandlerTestBase<TDomainModelEntity> : TestCaseBase
    where TDomainModelEntity : IEntity
{
    protected readonly string AggregateId = TestHelper.RandomId();
    private readonly DomainEventHandlerBase<TDomainModelEntity>.SupportedAggregateTypes _reflectionSupportedAggregateTypes = new();
    private readonly bool _reflectionIsStateInitializer;
    private readonly bool _reflectionIsOptionalStateMutation;

    protected IDomainEventHandler<TDomainModelEntity> Sut {
        get;
        init {
            field = value;
            var handlerType = typeof(DomainEventHandlerBase<TDomainModelEntity>);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            _reflectionSupportedAggregateTypes =
                (DomainEventHandlerBase<TDomainModelEntity>.SupportedAggregateTypes)(handlerType
                    .GetField("<supportedAggregateTypes>P", flags)
                    ?.GetValue(value) ?? new DomainEventHandlerBase<TDomainModelEntity>.SupportedAggregateTypes());

            _reflectionIsStateInitializer =
                (bool)(handlerType.GetField("<isStateInitializer>P", flags)
                    ?.GetValue(value) ?? false);

            _reflectionIsOptionalStateMutation = IsAssignableToOpenGenericBaseType(value.GetType(),
                typeof(OptionalStateMutationDomainEventHandlerBase<>));
        }
    } = null!;

    private static bool IsAssignableToOpenGenericBaseType(Type? type, Type openGenericBaseType)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openGenericBaseType)
            {
                return true;
            }
        }

        return false;
    }

    protected Event FixtureEvent(bool validTypeAggregate = true, bool validTypeEvent = true,
                                 object? payloadOverride = null, bool successPayload = true)
    {
        var typeAggregate = validTypeAggregate ?
            _reflectionSupportedAggregateTypes.Keys.First() :
            TestHelper.RandomId("unsupported_aggregate");

        _reflectionSupportedAggregateTypes.TryGetValue(typeAggregate, out var supportedEventTypes);

        var typeEvent = validTypeEvent ?
            supportedEventTypes is { Count: > 0 } ? supportedEventTypes.First() : DummyTypeEvent.Unknown.ToDescription() :
            TestHelper.RandomId("unsupported_event");

        return new Event(
            typeAggregate,
            AggregateId,
            typeEvent,
            payloadOverride ?? FixtureEventPayload(successPayload)
        );
    }

    protected virtual IEventEnvelopePayload? FixtureEventPayload(bool successPayload = true)
    {
        return null;
    }

    protected abstract TDomainModelEntity FixtureModelEntity();

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void CanHandle_ReturnsFalse_WhenEventIsNotSupported(bool validTypeAggregate, bool validTypeEvent)
    {
        Assert.False(Sut.CanHandleEvent(FixtureEvent(validTypeAggregate, validTypeEvent), FixtureModelEntity()));
    }

    [Fact]
    public void CanHandle_ReturnsFalse_WhenEventIsSupported_ButStateIsNotProvided_ToNonInitializer()
    {
        if (_reflectionIsStateInitializer || _reflectionIsOptionalStateMutation) return;

        Assert.False(Sut.CanHandleEvent(FixtureEvent(validTypeAggregate: true, validTypeEvent: true), default));
    }

    [Fact]
    public void CanHandle_WhenEventIsSupported_ButStateIsProvided_ToInitializer()
    {
        if (!_reflectionIsStateInitializer) return;

        Assert.True(Sut.CanHandleEvent(FixtureEvent(validTypeAggregate: true, validTypeEvent: true),
            FixtureModelEntity()));
    }

    [Fact]
    public void CanHandle_ReturnsTrue_WhenEventIsSupported_ByNonStateInitializer()
    {
        if (_reflectionIsStateInitializer) return;

        Assert.True(Sut.CanHandleEvent(FixtureEvent(validTypeAggregate: true, validTypeEvent: true),
            FixtureModelEntity()));
    }

    [Fact]
    public void CanHandle_ReturnsTrue_WhenEventIsSupported_ByStateInitializer()
    {
        if (!_reflectionIsStateInitializer) return;

        Assert.True(Sut.CanHandleEvent(FixtureEvent(validTypeAggregate: true, validTypeEvent: true), default));
    }

    [Fact]
    public void CanHandle_ReturnsTrue_WhenEventIsSupported_ButStateIsNotProvided_ToOptionalStateMutationHandler()
    {
        if (!_reflectionIsOptionalStateMutation) return;

        Assert.True(Sut.CanHandleEvent(FixtureEvent(validTypeAggregate: true, validTypeEvent: true), default));
    }

    [Fact]
    public async Task HandleAsync_WhenEntityIsMissing_ThrowsException()
    {
        if (_reflectionIsStateInitializer) return;

        if (_reflectionIsOptionalStateMutation)
        {
            await Assert.ThrowsAsync<NoStateFoundForOptionalEventHandlerException>(() =>
                Sut.HandleAsync(FixtureEvent(), default));

            return;
        }

        await Assert.ThrowsAsync<ArgumentNullException>(() => Sut.HandleAsync(FixtureEvent(), default));
    }

    [Fact]
    public void Sut_ExposesSupportedAggregateTypesThroughReflection()
    {
        // Guards the reflection used above: if the primary-constructor capture of DomainEventHandlerBase
        // changes, every other test in this class would silently become meaningless.
        Assert.NotEmpty(_reflectionSupportedAggregateTypes);
    }
}
