@description('The name of the Azure Cache for Redis instance')
param redisCacheName string

@description('The location for the Redis Cache')
param location string

resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: redisCacheName
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
}

output redisConnectionString string = '${redisCache.properties.hostName},abortConnect=false,ssl=true,password=${redisCache.listKeys().primaryKey}'
