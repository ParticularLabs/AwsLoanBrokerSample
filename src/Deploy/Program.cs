using Amazon.CDK;

var app = new App();

_ = new LoanBrokerStack(app, "LoanBroker");
//new LambdaStack(app, "CreditBureau", new StackProps());

app.Synth();