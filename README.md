# Custom RateLimitMiddleWare for ASP.NET Core

- used `ConcurrentDictionary<string, Queue<DateTime>>` [ *O(1) search* ] (which is thread safe) to hold all requests based on IP address.

- used datastruct `Queue` FIFO [ *O(1) insertion and O(1) deleteion* ] to track request count in specified time range

