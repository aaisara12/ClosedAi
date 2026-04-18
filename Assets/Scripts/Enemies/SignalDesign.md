# Overview
I'm currently working on a 3D game in Unity, which has a "signal" system
which the players and enemies can interact with. 

Signals are basically a graph which connects enemies to other enemies, and are
rendered in-game as translucent lines between enemies which are connected. 
Enemies which are connected to each other can use strategies. For example, 
if one enemy sees the player, all enemies which are connected will also become
aware of the player even if they do not have direct line of sight.

I need to implement Monobehaviors which implements the signal system (just the 
signal graph, we will work on strategies later).

# Specifications
Each `SignalManager` is attached to an enemy, and uses `Signal` objects to connect
to each other. 
* When a `Signal` is broken, the `SignalManager` will wait for a specified number of seconds
before trying to reform the `Signal`.
* To form a connection, it will query for other SignalManagers within a certain radius, 
and attempt to connect to each of them. If successful, create a new `Signal` object 
representing this link.
* If *all* `Signal`s are broken, disable the `SignalManager` for a certain number of seconds.

Each `Signal` is a simple script for rendering the line visually (may need to be implemented 
    via the Unity editor), holding the connection's hitbox, and holding references to the two `SignalManagers` 
    being connected.
* `Signal` connections between enemies need a hitbox, so the player can interact with them. 
* `Signal` connections can pass through their walls, so they likely need their own collision layer.

## Interfaces
`SignalManager`:
1. a needs a callback or event which other scripts can subscribe to. This event will fire 
    when a connection is made or broken.
2. a callback/event which will fire when all `Signal` connections are broken.
3. a function which will return all enemies in the signal graph.
4. a function which will return all directly connected enemies.
5. a function for receiving a connection, which the signalmanager can reject if it is at capacity.
6. a function for trying to connect with another signalManager

`Signal`:
1. Needs references to the two `SignalManager`s being connected.
2. May need a reference to collider to handle being destroyed.
