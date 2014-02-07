﻿using UnityEngine;
using System;

[Serializable]
public class SUISprite {
	[SerializeField]private Vector2 texCoords;
	[SerializeField]private Vector2 size;

	/** Draw the texture */
	public void Draw(Vector2 position, Texture texture) {
		if(texture == null) {
			Debug.LogWarning("A SUI Sprite is missing a texture");
			return;
		}

		GUI.DrawTextureWithTexCoords(
			new Rect(position.x, position.y, size.x, size.y),
		    texture,
		    new Rect((texCoords.x / 256) * size.x, (texCoords.y / 256) * size.y, size.x / 256, size.y / 256)
		);
	}

	/** Set or get the texture coordinates of the sprite */
	public Vector2 TexCoords {
		set { texCoords = value; }
		get { return texCoords; }
	}

	/** Set or get the size of the sprite */
	public Vector2 Size {
		set { size = value; }
		get { return size; }
	}
}