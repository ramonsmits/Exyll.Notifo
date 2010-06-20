
A Notifo API wrapper that only requires the .net v4.0 client profile framework.

The following  sample is also included in the (xml) documentation.

    var notifo = new NotifoClient(username, secret);

    notifo.SendNotification(
        username,
        "Testing 1, 2, 3 @ " + DateTime.Now,
        "Meet the creator",
        "http://maps.google.com/maps?q=Rotterdam,Netherlands",
        "Demo");

Any suggestions are welcome at notifo@ramonsmits.com