# HTTP-2-server

An implementation of the [HTTP/2 protocol](https://tools.ietf.org/html/rfc7540) in .NET Core

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See usage for notes on how to use the library in your system.

### Prerequisites

To run a server based on this library you must have [.NET Core Runtime 2.1-Preview2](https://github.com/dotnet/core/blob/master/release-notes/download-archives/2.1.0-preview2-download.md) installed, as this is, as of today, the only version of .NET Core supporting SSL connection with ALPN negotiation.

As this library runs on a preview of the latest .NET Core, there is no support for continuous integartion.


### Installing

Clone solution of of Github. 


## Usage
### Server
#### Contructors
|Name|Description|
|---------|---------|
|```Server()```|Initializes a new instance of the Server class that finds a local IP and sets certificat to default = null.|
|```Server(String)```|Initializes a new instance of the Server class and sets the specific IP. Certificat is set to default = null.|
|```Server(X509Certificate2)```|Initializes a new instance of the Server class, finds a local IP and sets certificat to the specified X509Certificate2.|
|```Server(String,X509Certificate2)```|Initializes a new instance of the Server class, sets specified IP and sets certificat to the specified X509Certificate2.|

#### Properties
|Name|Description|
|---------|---------|
|```Port```|Returns the port the server is listening to.|

#### Methods
|Name|Description|
|---------|---------|
|```Listen(int)```|Starts listening to given port.|
|```Get(string, RestURI.HTTPMethod)```|Returns the data from given url.|
|```Post(string, RestURI.HTTPMethod)```|Posts data to given url.|

#### Example
```cs
using System;
using System.Security.Cryptography.X509Certificates;
using lib;

namespace Example
{
    class ExampleServer
    {
        static void Main(string[] args)
        {
            //Creating the certificate
            var serverCertificate = new X509Certificate2("Certificate/TcpTLSServer_TemporaryKey.pfx", "1234");
            //Creating the server
            Server server = new Server( serverCertificate); // serverCertificate);
            //Server starts listening to port, and responding to webpage.
            server.Listen(443);
        }
    }
}
```


## Running the tests

Tests for this library is placed in its own project, UnitTesting.csproj. To run our tests you can open the solution in in Visual Studio 2017 and eighter;
* Ctrl+R,A
* press "Test", "Run", "All Tests". As shown  
![Where to find test](https://image.ibb.co/gZjKTc/TestVS.png)
`

### Tests

TestHPack(), tests encoding and decoding of headers

```cs
Http2.Hpack.Decoder decoder = new Http2.Hpack.Decoder();
Http2.Hpack.Encoder encoder = new Http2.Hpack.Encoder();
...
Http2.Hpack.Encoder.Result encodeResult = encoder.EncodeInto(headerBlockFragment, headers);
Http2.Hpack.DecoderExtensions.DecodeFragmentResult decodeResult = decoder.DecodeHeaderBlockFragment(new ArraySegment<byte>(buffer, 0, buffer.Length), maxHeaderFieldsSize, headers);
```
TestAddSettingsPayload(), adds settingspayload to a frame and then reads it out.
```cs
var settings = new(ushort, uint)[] { (SETTINGS_INITIAL_WINDOW_SIZE, 0x1000), (SETTINGS_ENABLE_PUSH, 0x0) };
...
SettingsPayload sp = frame.GetSettingsPayloadDecoded();
Assert.Equal(settings, sp.Settings);
```

TestAddRSTPayload(), adds reset paylaod to a frame and reads it out.
```cs
frame.AddRSTStreamPayload(error);
...
RSTStreamPayload rp = frame.GetRSTStreamPayloadDecoded();
Assert.Equal(error, rp.ErrorCode);
```
TestAddPushPromisePayload(), adds pushpromise paylaod to a frame and reads it out.
```cs
frame.AddPushPromisePayload(psi, hbf, endHeaders: true);
...
PushPromisePayload pp = frame.GetPushPromisePayloadDecoded();
Assert.Equal(psi, pp.PromisedStreamID);
Assert.Equal(hbf, pp.HeaderBlockFragment);
Assert.Equal(0, pp.PadLength);
```
TestAddDataPayload(), adds pushpromise paylaod to a frame and reads it out.
```cs
frame.AddDataPayload(ExtractBytes(data), paddingLength:16);
...
dp = frame.GetDataPayloadDecoded();
Assert.Equal(ExtractBytes(data), dp.Data);
Assert.Equal(0, dp.PadLength);
```

TestSplit32BitToBoolAnd31bitInt(), seperates the first bit from different integers.
```cs
uint _uint = 0b10000000000000000000000000000000;
int test = (int)(_uint | 0b01111000000000000000000000000000); // 1 and 2013265920
..
var t = Split32BitToBoolAnd31bitInt(test);
Assert.True(t.bit32);
Assert.True(2013265920 == t.int31);
...
```

TestExtractBytes(), converts a long, a int and a short into byte arrays.
```cs
...
short s = 12364;
b = ExtractBytes(s);
Array.Reverse(b);
Assert.Equal(BitConverter.ToInt16(b, 0),s);
```

TestConvertFromIncompleteByteArray(), reverser a byte array and converts it back
```cs
int i = 1823423647;
var b = BitConverter.GetBytes(i);
Array.Reverse(b);
Assert.Equal(ConvertFromIncompleteByteArray(b), i);
```

TestConvertToBytes(), Converts integer to bytearray
```cs
int i = 19;
var b = BitConverter.GetBytes(i);
Array.Reverse(b);
Assert.Equal(b, ConvertToByteArray(i));
```

TestCombineHeaderPayloads(), combines the payloads from different headerframes to get the complete headerlist.
```cs
...
var continuation = new HTTP2Frame(28).AddContinuationFrame(continuationData, true);
var total = CombineHeaderPayloads(header, continuation);
...
Assert.Equal(CombineByteArrays(headerData, continuationData),total);
```

TestPriorityPayload(), adds priotiry payload to frame.
```cs
...
PriorityPayload pp = frame.GetPriorityPayloadDecoded();
Assert.Equal(sid, pp.StreamDependency);
Assert.True(pp.StreamDependencyIsExclusive);
Assert.Equal(28, pp.Weight);
```

TestHeaderPayload(), adds header payload to frame.
```cs
 byte[] data = { 1, 2, 3, 4 };
HTTP2Frame frame = new HTTP2Frame(1).AddHeaderPayload(data, 2, true, true);
HeaderPayload hh = frame.GetHeaderPayloadDecoded();
```
TestRestURI(), adds several GET-methods to different URLs and and then checks if they are there.
```cs
...
RestLibrary.AddURI("GET", "shoppinglists/", (req, res) => res.Send("List of shoppinglists"));
...
Assert.True(RestLibrary.HasMethod("GET", "shoppinglists"));
...

```

## Implementations

### Establishing a connection over HTTP/2
* Starting HTTP/2 for "http" URIs, ref [RFC7540 Section 3.2](https://tools.ietf.org/html/rfc7540#section-3.2)
* Starting HTTP/2 for "https" URIs, ref [RFC7540 Section 3.3](https://tools.ietf.org/html/rfc7540#section-3.3)
* HTTP/2 Connection Preface, ref [RFC7540 Section 3.5](https://tools.ietf.org/html/rfc7540#section-3.5)

### Frames
* Frame format (Encoding and Decoding), ref [RFC7540 Section 4.1](https://tools.ietf.org/html/rfc7540#section-4.1)
* Header Compression and Decompression (from NuGet), ref [RFC7540 Section 4.3](https://tools.ietf.org/html/rfc7540#section-4.3)

#### Frame Definitions [RFC7540 Section 6](https://tools.ietf.org/html/rfc7540#section-6)
* All.

### Multiplexing and Streams

* Stream Identifiers, ref [RFC7540 Section 5.1.1](https://tools.ietf.org/html/rfc7540#section-5.1.1)
* Stream Concurrency, ref [RFC7540 Section 5.1.2](https://tools.ietf.org/html/rfc7540#section-5.1.2)
* Stream Dependencies, ref [RFC7540 Section 5.3.1](https://tools.ietf.org/html/rfc7540#section-5.3.1)

### HTTP Message Exchange
* Server Push (Depending on Push_Promise)[RFC7540 Section 8.2](https://tools.ietf.org/html/rfc7540#section-8.2)

### Other implementations
* REST-services
* GZip encoding (might be a bit bugged).
* ...

## Limitations and further development
* Find out why Push_Promise doesn't work.
* Further implementations of Stream states, and dependency weighting.
* Error handeling
* Research why some browsers cooperate with our example server better than others.
* Cleanup classes
* Further work on flowcontroll and recieving data from client.
* Further debugging
* Write more testes (e.g. end to end tests)
* Create a better testing enviorment. 

## Built With

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
* A thanks to [samuelneff](https://github.com/samuelneff) whose Mime type mapping we used in this project.

