# HeroKnight-ML
A 2D platform that uses MLAgents for AI

#### Idea:
The idea to start this training environment comes from Immersive Limit's platformer challenge.
[Immersive Limit](https://community.immersivelimit.com/t/ml-agents-platformer/22)

#### Process:
Using reference from [Hummingbird ML Tutorial](https://learn.unity.com/course/ml-agents-hummingbirds)
 I turned the basic HeroKnight.cs script into a HeroKnightAgent.cs. 
 Compare HeroKnight.cs with HeroKnightAgent.cs to see the few changes that had to take place.
 
 #### Issues:
 1. The first issue I ran into during training was that after the agent moved left and right a few times, all of the sudden, the agent would be stuck jumping up and down and turning left and right, but not moving left-right. 
    After considering the .config file as a culprit, I realized that the bug was coded into the roll animation. The HeroKnight.cs script is a MonoBehaviour and it is able to link to an animation event. In this case, the last frame of the roll animation would call a funciton in HeroKnight.cs to set `m_rolling` to false, which would then allow left-right movement to take place. Yet because HeroKnight.cs became HeroKnightAgent.cs and the `MonoBehaviour` became `Agent`, the animation event was unlinked, thus no longer called the said function to set `m_rolling` to false. To fix this I removed all notions of rolling for now. 
 2. Training ... more info to come
 

#### Packages:
[Hero Knight - Pixel Art](https://assetstore.unity.com/packages/2d/characters/hero-knight-pixel-art-165188)

[2D Pixel Item Asset Pack](https://assetstore.unity.com/packages/2d/gui/icons/2d-pixel-item-asset-pack-99645)

Cinemachine 2.6.3
MLAgents 1.0.6
2D Tilemap Editor 1.0.0


