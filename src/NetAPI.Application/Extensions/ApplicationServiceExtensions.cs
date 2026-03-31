using FluentValidation;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NetAPI.Application.Behaviors;
using NetAPI.Application.DTOs;
using NetAPI.Domain.Entities;
using System.Reflection;

namespace NetAPI.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceExtensions).Assembly;

        // MediatR with ordered pipeline behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // FluentValidation — auto-register all validators in the assembly
        services.AddValidatorsFromAssembly(assembly);

        // Mapster — register type mapping config
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);

        // Configure ProductDto mapping (flatten Money value object)
        config.NewConfig<Product, ProductDto>()
            .Map(dest => dest.Price, src => src.Price.Amount)
            .Map(dest => dest.Currency, src => src.Price.Currency);

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
