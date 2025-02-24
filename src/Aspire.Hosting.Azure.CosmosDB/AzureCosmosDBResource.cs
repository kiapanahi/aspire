// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.CosmosDB;

namespace Aspire.Hosting;

/// <summary>
/// A resource that represents an Azure Cosmos DB.
/// </summary>
public class AzureCosmosDBResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure),
    IResourceWithConnectionString,
    IResourceWithEndpoints
{
    internal List<CosmosDBDatabase> Databases { get; } = [];

    internal EndpointReference EmulatorEndpoint => new(this, "emulator");

    /// <summary>
    /// Gets the "connectionString" reference from the secret outputs of the Azure Cosmos DB resource.
    /// </summary>
    public BicepSecretOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets the "connectionString" output reference from the bicep template for the Azure Cosmos DB resource.
    /// 
    /// This is used when Entra ID authentication is used. The connection string is an output of the bicep template.
    /// </summary>
    public BicepOutputReference ConnectionStringOutput => new("connectionString", this);

    /// <summary>
    /// Gets the "connectionString" secret output reference from the bicep template for the Azure Redis resource.
    ///
    /// This is set when access key authentication is used. The connection string is stored in a secret in the Azure Key Vault.
    /// </summary>
    internal BicepSecretOutputReference? ConnectionStringSecretOutput { get; set; }

    /// <summary>
    /// Gets a value indicating whether the resource uses access key authentication.
    /// </summary>
    [MemberNotNullWhen(true, nameof(ConnectionStringSecretOutput))]
    public bool UseAccessKeyAuthentication => ConnectionStringSecretOutput is not null;

    /// <summary>
    /// Gets a value indicating whether the Azure Cosmos DB resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    internal bool IsPreviewEmulator =>
        this.TryGetContainerImageName(out var imageName) &&
        imageName == $"{CosmosDBEmulatorContainerImageTags.Registry}/{CosmosDBEmulatorContainerImageTags.Image}:{CosmosDBEmulatorContainerImageTags.TagVNextPreview}";

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Cosmos DB resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        IsEmulator
        ? AzureCosmosDBEmulatorConnectionString.Create(EmulatorEndpoint, IsPreviewEmulator)
        : UseAccessKeyAuthentication ?
            ReferenceExpression.Create($"{ConnectionStringSecretOutput}") :
            ReferenceExpression.Create($"{ConnectionStringOutput}");

}
