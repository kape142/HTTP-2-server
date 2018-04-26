# HTTP-2-server

An implementation of the [HTTP/2 protocol](https://tools.ietf.org/html/rfc7540) in .NET Core

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system.

### Prerequisites

To run a server based on this library you must have [.NET Core Runtime 2.1-Preview2](https://github.com/dotnet/core/blob/master/release-notes/download-archives/2.1.0-preview2-download.md) installed, as this is, as of today, the only version of .NET Core supporting SSL connection with ALPN negotiation.



### Installing

A step by step series of examples that tell you have to get a development env running

Create an instance of the Server

```cs
var serverCertificate = new X509Certificate2("Certificate/TcpTLSServer_TemporaryKey.pfx", "1234");
Server server = new Server(serverCertificate);
```

Have server start listening

```cs
//parameter is the port you wish to listen to
server.listen(443);
```

## Running the tests

Explain how to run the automated tests for this system

### Break down into end to end tests

Explain what these tests test and why

```
Give an example
```

### And coding style tests

Explain what these tests test and why

```
Give an example
```

## Deployment

Add additional notes about how to deploy this on a live system


## Implementations

### Establshing a connection over HTTP/2
* Starting HTTP/2 for "http" URIs, ref [RFC7540 Section 3.2](https://tools.ietf.org/html/rfc7540#section-3.2)
* Starting HTTP/2 for "https" URIs, ref [RFC7540 Section 3.3](https://tools.ietf.org/html/rfc7540#section-3.3)
* HTTP/2 Connection Preface, ref [RFC7540 Section 3.5](https://tools.ietf.org/html/rfc7540#section-3.5)

### Frames
* Frame format (Encoding and Decoding), ref [RFC7540 Section 4.1](https://tools.ietf.org/html/rfc7540#section-4.1)
* Header Compression and Decompression (from NuGet), ref [RFC7540 Section 4.3](https://tools.ietf.org/html/rfc7540#section-4.3)

#### Frame Definitions [RFC7540 Section 6](https://tools.ietf.org/html/rfc7540#section-6)
All.

### Multiplexing and Streams

* Stream Identifiers, ref [RFC7540 Section 5.1.1](https://tools.ietf.org/html/rfc7540#section-5.1.1)
* Stream Concurrency, ref [RFC7540 Section 5.1.2](https://tools.ietf.org/html/rfc7540#section-5.1.2)
* Stream Dependencies, ref [RFC7540 Section 5.3.1](https://tools.ietf.org/html/rfc7540#section-5.3.1)

### HTTP Message Exchange
* Server Push [RFC7540 Section 8.2](https://tools.ietf.org/html/rfc7540#section-8.2)

## Limitations and further development

* Further implementations of Stream states, and dependency weighting.
* Error handeling
* Work out why some browsers cooperate with our example server better than others.
* Cleanup classes
* Implement continous integration for project.
* Further work on flowcontroll and recieving data from client.
* Further debugging
* Write more testes
* Create a better testing enviorment. 

## Built With

* [Dropwizard](http://www.dropwizard.io/1.0.2/docs/) - The web framework used
* [NuGet](https://nuget.org/) - Dependency Management

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/kape142/HTTP-2-server/tags). 

## Authors

* **Karl Peter Skjelvik** - *Initial work* - [Kape142](https://github.com/kape142)
* **Jone Vassbø** - *Initial work* - [JoneV](https://github.com/jonev)
* **Martin Wangen** - *Initial work* - [SulFaX](https://github.com/sulfax)

See also the list of [contributors](https://github.com/kape142/HTTP-2-server/graphs/contributors) who participated in this project.

## Acknowledgments

* A thanks to [Matthias247](https://github.com/Matthias247/) whose hpack implementation we used in this project to compress headers.

