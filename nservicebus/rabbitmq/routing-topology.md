---
title: Routing topology
reviewed: 2016-08-30
component: Rabbit
versions: '[2,]'
---


The RabbitMQ transport has the concept of a routing topology, which controls how it creates exchanges, queues, and the bindings between them in the RabbitMQ broker. The routing topology also controls how the transport uses the exchanges it creates to send and publish messages.

## Conventional Routing Topology

By default, the RabbitMQ transport uses the `ConventionalRoutingTopology`, which creates separate [fanout exchanges](https://www.rabbitmq.com/tutorials/amqp-concepts.html#exchange-fanout) for each message type, including inherited types, being published in the system. This means that polymorphic routing and multiple inheritance for events is supported since each subscriber will bind its input queue to the relevant exchanges based on the event types that it has handlers for. It also created dedicated exchange for each endpoint in the system that is used during sends. Exchanges are created:
 - Exchanges for events published in the system are created on the first publish or during subscription 
 - Exchanges for sends to give endpoint are created when endpoint is starting.

### Example

In the example 3 endpoints will be analyzed:
 - Publisher
 - Subscriber
 - SpecificSubscriber

In this system the following event hierarchy will be published by Publisher:
 - SpecificBusinessEvent
  - GeneralBusinessEvent   

Where `SpecificBusinessEvent` derives from `GeneralBusinessEvent`. And the following messages will be send:
 - SomeMessage

Before running 3 endpoints the Rabbit broker does not have anything configured.

| Exchanges | Bindings | Queues |
| --------- | -------- | ------ |

 1. Publisher is starting - during start-up input queue is created and an exchange with the name matching endpoint name.

| Exchanges | Bindings | Queues |
| Publisher | Publisher to publisher binding | Publisher |

 2. Subscriber is starting - during start-up input queue is created and an exchange with the name matching endpoint name.

| Exchanges | Bindings | Queues |
| Publisher | Publisher to publisher binding | Publisher |
| Subscriber | Subscriber to subscriber binding | Subscriber |

 3. Subscriber subscribes to GeneralBusinessEvent - during this step an exchange for that event and it's base classes will be created as well as a binding will be set up.

| Exchanges | Bindings | Queues |
| Publisher | Publisher to publisher binding | Publisher |
| Subscriber | Subscriber to subscriber binding | Subscriber |
| GeneralBusinessEvent | GeneralBusinessEvent to subscriber binding |  |

 4. SpecificSubscriber is starting - during start-up input queue is created and an exchange with the name matching endpoint name.

| Exchanges | Bindings | Queues |
| Publisher | Publisher to publisher binding | Publisher |
| Subscriber | Subscriber to subscriber binding | Subscriber |
| GeneralBusinessEvent | GeneralBusinessEvent to subscriber binding |  |
| SpecificSubscriber | SpecificSubscriber to SpecificSubscriber binding | SpecificSubscriber |

 5. SpecificSubscriber subscribes to SpecificBusinessEvent - during this step an exchange for that event and it's base classes will be created as well as a binding will be set up. 

| Exchanges | Bindings | Queues |
| Publisher | Publisher to publisher binding | Publisher |
| Subscriber | Subscriber to subscriber binding | Subscriber |
| GeneralBusinessEvent | GeneralBusinessEvent to subscriber binding |  |
| SpecificSubscriber | SpecificSubscriber to SpecificSubscriber binding | SpecificSubscriber |
| SpecificBusinessEvent | SpecificBusinessEvent to SpecificSubscriber binding |  |
| SpecificBusinessEvent | SpecificBusinessEvent to GeneralBusinessEvent binding |  |
 
6. Publisher publish SpecificBusinessEvent - the code checks if exchanges exists and as they do the event is published to exchange matching the type of the message: SpecificBusinessEvent.

WARNING: The RabbitMQ transport doesn't automatically modify or delete existing bindings. Because of this, when modifying the message class hierarchy, the existing bindings for the previous class hierarchy will still exist and should be deleted manually.


## Direct Routing Topology

The `DirectRoutingTopology` is another provided routing topology that routes all events through a single exchange, `amq.topic` by default. Events are published using a routing key based on the event type, and subscribers will use that key to filter their subscriptions.

To enable the direct routing topology, use the following configuration:

snippet:rabbitmq-config-usedirectroutingtopology

Adjust the conventions for exchange name and routing key by using the overload:

snippet:rabbitmq-config-usedirectroutingtopologywithcustomconventions

### Example

Lets use the same case as before. At the beginning Rabbit broker does not have anything configured:

| Exchanges | Bindings | Queues |
| --------- | -------- | ------ |

 1. Publisher is starting - during start-up input queue is created.

| Exchanges | Bindings | Queues |
|  |  | Publisher |

 2. Subscriber is starting - during start-up input queue is created.

| Exchanges | Bindings | Queues |
|  |  | Publisher |
|  |  | Subscriber |

 3. Subscriber subscribes to GeneralBusinessEvent - during this step a binding will be set up between default exchange and an input queue of that endpoint for given routing key.

| Exchanges | Bindings | Queues |
|  |  | Publisher |
|  |  | Subscriber |
|  | Default exchange to subscriber binding using `GeneralBusinessEvent` as a routing key |  |

 4. SpecificSubscriber is starting - during start-up input queue is created 

| Exchanges | Bindings | Queues |
|  |  | Publisher |
|  |  | Subscriber |
|  | Default exchange to subscriber binding using `GeneralBusinessEvent` as a routing key |  |
|  |  | SpecificSubscriber |

 5. SpecificSubscriber subscribes to SpecificBusinessEvent - during this step a binding will be set up between default exchange and an input queue of that endpoint for given routing key. 

| Exchanges | Bindings | Queues |
|  |  | Publisher |
|  |  | Subscriber |
|  | Default exchange to subscriber binding using `GeneralBusinessEvent` as a routing key |  |
|  |  | SpecificSubscriber |
|  | Default exchange to SpecificSubscriber binding using `SpecificBusinessEvent` as a routing key |  |
 
6. Publisher publish SpecificBusinessEvent - event is published to default exchange using as a routing key `SpecificBusinessEvent`

## Custom Routing Topology

If the routing topologies mentioned above aren't flexible enough, then take full control over how routing is done by implementing a custom routing topology. This is done by:

 1. Define the topology by creating a class implementing `IRoutingTopology`.
 1. Register it with the transport calling `UseRoutingTopology` as shown below.

snippet:rabbitmq-config-useroutingtopology