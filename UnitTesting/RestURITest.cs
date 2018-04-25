using System;
using Xunit;
using lib;
using lib.HTTPObjects;
using lib.Frames;
using static lib.RestURI;

namespace UnitTesting
{
    public class RestURITest
    {
        [Fact]
        public void TestRestURI()
        {
            RestLibrary.AddURI("GET", "shoppinglists/favourite/:householdid/username/shoppinglistid", (req, res) => res.Send($"HouseholdID: {req.Params["householdid"]}, username: {req.Params["username"]}, shoppinglistid: {req.Params["shoppinglistid"]}"));
            RestLibrary.AddURI("GET", "shoppinglists/favourite/:householdid", (req, res) => res.Send($"List of favourite shoppinglists in household {req.Params["householdid"]}"));
            RestLibrary.AddURI("GET", "shoppinglists/", (req, res) => res.Send("List of shoppinglists"));

            Assert.True(RestLibrary.HasMethod("GET", "shoppinglists/favourite/householdid/username/shoppinglistid"));
            Assert.True(RestLibrary.HasMethod("GET", "shoppinglists/favourite/householdid"));
            Assert.True(RestLibrary.HasMethod("GET", "shoppinglists"));

            Assert.False(RestLibrary.HasMethod("GET", "shoppinglists/favourite/householdid/username"));
            Assert.False(RestLibrary.HasMethod("POST", "shoppinglists/favourite/householdid"));

            RestLibrary.Execute("GET", "shoppinglists/favourite/23/kaviar/45", new Request(), new MockResponse("HouseholdID: 23, username: kaviar, shoppinglistid: 45"));
            RestLibrary.Execute("GET", "shoppinglists/favourite/12", new Request(), new MockResponse("List of favourite shoppinglists in household 12"));
            RestLibrary.Execute("GET", "shoppinglists", new Request(), new MockResponse("List of shoppinglists"));

            Assert.Throws<ArgumentException>(() => RestLibrary.Execute("GET", "shoppinglists/favourite/23/username", new Request(), new MockResponse("should crash, not read this")));
        }

        private class MockResponse : IResponse
        {
            private String output;

            public MockResponse(string output)
            {
                this.output = output;
            }

            public void Send(string data)
            {
                Assert.Equal(output, data);
            }
        }
    }
}
