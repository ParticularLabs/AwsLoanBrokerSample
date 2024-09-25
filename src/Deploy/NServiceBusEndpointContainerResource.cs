using Amazon.CDK;
using Amazon.CDK.AWS.ECS;

namespace Deploy;

// internal class NServiceBusEndpointContainerResource : NServiceBusEndpointResource
// {
//     public NServiceBusEndpointContainerResource(EndpointDetails endpoint, ContainerDefinitionProps containerProps, Construct scope, string id, IStackProps? props)
//         : base(endpoint, scope, id, props)
//     {
//         // Queues created by base class
//
//         var container = new ContainerDefinition(this, "container", containerProps);
//     }
// }