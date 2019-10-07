﻿using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class World : MonoBehaviour
{
	public GameObject chunkPrefab;
	public GameObject playerPrefab;
	
	public Player player { get; private set; }

	private Dictionary<int2, Chunk> _chunks = new Dictionary<int2, Chunk>();
	private GenerationQueue _genQueue = new GenerationQueue();

	private List<int2> _chunkRemoveList = new List<int2>();
	
	private const float RENDER_DISTANCE = 24 - 0.1f;

	private void Start()
	{
		Toolbox.world = this;
		
		Biomes.initSeed();
		
		player = Instantiate(playerPrefab, new float3(8, 256, 8), Quaternion.identity).GetComponent<Player>();
		player.setWorld(this);
	}

	private void createChunk(int2 chunkPos)
	{
		if (_chunks.ContainsKey(chunkPos))
		{
			Debug.LogError("A chunk has been overriden without prior deletion");
			destroyChunk(chunkPos);
		}
		
		Chunk chunk = Instantiate(chunkPrefab, new Vector3Int(chunkPos.x, 0, chunkPos.y) * 16, Quaternion.identity).GetComponent<Chunk>();
		
		_chunks.Add(chunkPos, chunk);
		
		chunk.init(chunkPos);
		
		_genQueue.enqueue(chunk);
	}

	private void destroyChunk(int2 chunkPos)
	{
		if (!_chunks.ContainsKey(chunkPos))
		{
			Debug.LogError("Deletion of a non-existing chunk has been requested");
			return;
		}

		Destroy(_chunks[chunkPos].gameObject);
		
		_chunkRemoveList.Add(chunkPos);
	}

	public void placeBlock(int3 blockPos, byte blockType)
	{
		int2 chunkPos = chunkPosFromBlockPos(blockPos);

		if (!_chunks.ContainsKey(chunkPos))
		{
			Debug.LogError("Chunk where block is placed doesn't exists");
			return;
		}

		_chunks[chunkPos].placeBlock(localBlockPosFromBlockPos(blockPos), blockType);
	}

	private static int2 chunkPosFromBlockPos(int3 blockPos)
	{
		int2 chunkPos = int2.zero;

		chunkPos.x = Mathf.FloorToInt(blockPos.x / 16.0f);
		chunkPos.y = Mathf.FloorToInt(blockPos.z / 16.0f);

		return chunkPos;
	}

	private static int3 localBlockPosFromBlockPos(int3 blockPos)
	{
		int3 localBlockPos = int3.zero;
		
		localBlockPos.x = Mathf.RoundToInt(Mathf.Repeat(blockPos.x, 16));
		localBlockPos.y = blockPos.y;
		localBlockPos.z = Mathf.RoundToInt(Mathf.Repeat(blockPos.z, 16));

		return localBlockPos;
	}
	
	private static int2 chunkPosFromPlayerPos(float3 playerPos)
	{
		int2 chunkPos = int2.zero;

		chunkPos.x = Mathf.FloorToInt(playerPos.x / 16.0f);
		chunkPos.y = Mathf.FloorToInt(playerPos.z / 16.0f);

		return chunkPos;
	}

	private void FixedUpdate()
	{
		_genQueue.update();
		
		int2 chunkWithPlayer = chunkPosFromPlayerPos(player.transform.position);

		foreach (KeyValuePair<int2, Chunk> pair in _chunks)
		{
			if (math.distance(pair.Key, chunkWithPlayer) > RENDER_DISTANCE)
			{
				destroyChunk(pair.Key);
			}
		}

		foreach (int2 chunkPos in _chunkRemoveList)
		{
			_chunks.Remove(chunkPos);
		}
		_chunkRemoveList.Clear();

		for (int x = chunkWithPlayer.x ; x <= chunkWithPlayer.x + RENDER_DISTANCE; x++)
		{
			for (int y = chunkWithPlayer.y ; y <= chunkWithPlayer.y + RENDER_DISTANCE; y++)
			{
				if ((chunkWithPlayer.x - x) * (chunkWithPlayer.x - x) + (chunkWithPlayer.y - y) * (chunkWithPlayer.y - y) > RENDER_DISTANCE * RENDER_DISTANCE) continue;
				
				int xSym = chunkWithPlayer.x - (x - chunkWithPlayer.x);
				int ySym = chunkWithPlayer.y - (y - chunkWithPlayer.y);
				
				int2 pos1 = new int2(x, y);
				int2 pos2 = new int2(x, ySym);
				int2 pos3 = new int2(xSym, y);
				int2 pos4 = new int2(xSym, ySym);

				if (!_chunks.ContainsKey(pos1))
				{
					createChunk(pos1);
				}
				if (!_chunks.ContainsKey(pos2))
				{
					createChunk(pos2);
				}
				if (!_chunks.ContainsKey(pos3))
				{
					createChunk(pos3);
				}
				if (!_chunks.ContainsKey(pos4))
				{
					createChunk(pos4);
				}
			}
		}
		
		if (player.transform.position.y < -10)
		{
			Transform pTransform = player.transform;
			float3 position = pTransform.position;
			pTransform.position = new float3(position.x, player.spawnPos.y, position.z);
		}
		
		if (_genQueue.tryDequeue(out Chunk chunk))
		{
			if (chunk)
				chunk.applyMesh();
		}
	}
}