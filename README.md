# AWS LoanBroker Sample

The sample is a basic LoanBroker implementation following the [structure presented](https://www.enterpriseintegrationpatterns.com/patterns/messaging/ComposedMessagingExample.html) by [Gregor Hohpe](https://www.enterpriseintegrationpatterns.com/gregor.html) in his [Enterprise Integration Pattern](https://www.enterpriseintegrationpatterns.com/) book.

The sample is composed by:

- A client application, sending loan requests.
- A loan broker service, receiving loan requests and orchestrating communication with downstream banks.
- Three bank adapters, acting like Anti Corruption layers (ACL) towards three different banks offering loans.

## Requirements

- .NET 8 or greater
- Docker

## How to run the sample from the development IDE

The sample uses [LocalStack](https://www.localstack.cloud/) in a Docker container to mock AWS services. Using a command prompt, run LocalStack first by issuing the following command in the `src` directory:

```shell
docker-compose up localstack
```

The above command will execute the sample `docker-compose.yml` file starting only the LocalStack container.

Once the LocalStack container is up and running, from the development environment start the following projects:

- Client
- LoanBroker
- BankAdapter1
- BankAdapter2
- BankAdapter3

To stop the LocalStack container, at the command prompt issue the following command from the `src` folder:

```shell
docker-compose stop localstack
```

## How to run the sample using Docker containers

The client application, the LoanBroker service, and the bank adapters can be deployed as Docker containers alongside with the LocalStack one to mock the AWS services. To do, from the `src` folder, execute the following command:  

```shell
docker-compose up --build
```

The above command will build all projects, build container images for each of them, deploy them to the local Docker registry, and start them.

To run the solution without rebuilding container images, from the `src` folder, using a command prompt, execute the following command:

```shell
docker-compose up
```

The docker-compose configuration will start the following containers:

- LocalStack
- Client
- LoanBroker
- BankAdapter1
- BankAdapter2
- BankAdapter3

All containers will use the same network as the LocalStack container instance.

To interact with the sample, attach a console to the Client running container by executing the following command:

```shell
docker attach src-client-1
```

Attach and use the `F` key

```shell
docker-compose down
```

### Telemetry

NServiceBus supports OpenTelemetry.
All endpoints are configured to send telemetry data to Jaeger.

To visualize traces, open the [Jaeger dashboard](http://localhost:16686).

### Sample scenarios

TODO

- Press F on the client and observe messages flowing bla bla
- Stop all bank adapters, press F on the client and observe the behavior
- Stop the LoanBroker, press F on the client and stop the client, start the LoanBroker observe messages flowing, start the client and observe the Loanbroker response eventually coming in.

## How to modify the same to run agains an AWS Account

TODO
=======
# AwsLoanBrokerSample

## LocalStack setup

Docker compose file:

```
services:
  localstack:
    image: localstack/localstack
    environment:
      - SERVICES=sns,sqs,iam,s3,dynamodb
      - DEBUG=1
      - HOSTNAME=localstack
      - EDGE_PORT=4566
    ports:
      - '4566-4597:4566-4597'
      - "8000:5000"
```

Set the `ServiceUrl` of the various Amazon client config classes to `http://localhost:{EDGE_PORT}/`.
Set the various clients to use dummy `BasicAWSCredentials` :

```
var dummy = new BasicAWSCredentials("xxx","xxx");`
```

For example:

```
var edgeUrl = "http://localhost:4566";
var dummy = new BasicAWSCredentials("xxx","xxx");
var sqsConfig = new AmazonSQSConfig() { ServiceURL = edgeUrl };
var snsConfig = new AmazonSimpleNotificationServiceConfig(){ ServiceURL = edgeUrl };

var transport = new SqsTransport(
    new AmazonSQSClient(dummy, sqsConfig),
    new AmazonSimpleNotificationServiceClient(dummy, snsConfig));
```
The dummy credentials prevent the AWS clients from trying to pickup credentials from environment variables or connect to the AWS cloud IAM service to retrieve authorizations.
