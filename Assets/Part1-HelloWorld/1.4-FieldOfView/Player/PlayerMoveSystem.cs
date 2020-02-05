﻿
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RLTKTutorial.Part1_4
{
    public class PlayerMoveSystem : JobComponentSystem
    {
        EntityQuery _mapQuery;
        EntityQuery _playerQuery;

        protected override void OnCreate()
        {
            _mapQuery = GetEntityQuery(
                ComponentType.ReadWrite<TileBuffer>(),
                ComponentType.ReadOnly<MapData>()
                );

            _playerQuery = GetEntityQuery(
                ComponentType.ReadWrite<Position>(),
                ComponentType.ReadOnly<PlayerInput>(),
                ComponentType.ReadOnly<Player>());
            
            _playerQuery.AddChangedVersionFilter(ComponentType.ReadWrite<PlayerInput>());
            RequireForUpdate(_playerQuery);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (_playerQuery.CalculateEntityCount() == 0)
                return inputDeps;

            var playerEntity = _playerQuery.GetSingletonEntity();
            var inputFromEntity = GetComponentDataFromEntity<PlayerInput>(false);
            var posFromEntity = GetComponentDataFromEntity<Position>(false);

            // We want to do the foreach over the map ( as opposed to the player )
            // to avoid the "container is not suitable for parallel writing" 
            // from passing the map into a foreach. 
            // TODO : Clean this up a bit, maybe just get everything via query and 
            // do the work inside Job.WithCode
            inputDeps = Entities
                .WithNativeDisableParallelForRestriction(posFromEntity)
                .WithNativeDisableParallelForRestriction(inputFromEntity)
                .ForEach((ref DynamicBuffer<TileBuffer> map, in MapData mapData) =>
                {
                    var input = inputFromEntity[playerEntity];
                    var hor = input.movement.x;
                    var ver = input.movement.y;

                    var pos = posFromEntity[playerEntity];
                    int2 p = pos;

                    p.x += (int)hor;
                    p.y += (int)ver;

                    int idx = p.y * mapData.width + p.x;
                    if( idx >= 0 && idx < map.Length )
                    {
                        if (map[idx].value != TileType.Wall)
                        {
                            posFromEntity[playerEntity] = p;
                        }
                    }
                }).Schedule(inputDeps);
    
            return inputDeps;
        }
    }
}