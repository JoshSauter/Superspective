using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using Saving;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine.Serialization;

[RequireComponent(typeof(UniqueId))]
public class ValueDisplay : SaveableObject<ValueDisplay, ValueDisplay.ValueDisplaySave> {
	protected const int MIN = -80;
	protected const int MAX = 80;
	
	[FormerlySerializedAs("currentValueDisplayHi")]
	public SpriteRenderer currentValueDisplay;
	public SpriteRenderer currentValueNegativeSymbol;
	protected Sprite[] base9Symbols;
	
	public int actualValue = 0;
	protected float _displayedValue = 0f;

	public float displayedValue {
		get => _displayedValue;
		private set {
			value = Mathf.Clamp(value, MIN, MAX);
			if (value != _displayedValue) {
				int valueAsInt = Mathf.CeilToInt(Mathf.Abs(value));
				currentValueDisplay.sprite = base9Symbols[valueAsInt];
				
				bool isNegative = value < 0;
				currentValueNegativeSymbol.enabled = isNegative;

				_displayedValue = value;
			}
		}
	}
	
	public virtual void SetDisplayedValue(float value) {
		displayedValue = value;
	}

	[SerializeField]
	protected float _spriteAlpha = 1f;
	public float spriteAlpha {
		get => _spriteAlpha;
		set {
			if (value != _spriteAlpha) {
				currentValueDisplay.color = currentValueDisplay.color.WithAlpha(value);
				currentValueNegativeSymbol.color = currentValueNegativeSymbol.color.WithAlpha(value);

				_spriteAlpha = value;
			}
		}
	}

	[SerializeField]
	protected Color _color;
	public Color color {
		get => _color;
		private set {
			ApplyColors(value);
			_color = value;
		}
	}

	protected virtual void ApplyColors(Color to) {
		currentValueDisplay.color = to.WithAlpha(spriteAlpha);
		currentValueNegativeSymbol.color = to.WithAlpha(spriteAlpha);
	}

	public void SetColorImmediately(Color to) {
		_desiredColor = to;
		color = to;
	}
	
	public Color defaultColor = Color.black;
	protected Color _desiredColor;
	[ShowNativeProperty]
	public Color desiredColor {
		get => _desiredColor;
		set => _desiredColor = value;
	}
	public float lerpSpeed = 4f;

	protected override void OnValidate() {
		base.OnValidate();
		
		defaultColor = currentValueDisplay?.color ?? GetComponent<SpriteRenderer>().color;
		desiredColor = defaultColor;
		color = defaultColor;
		actualValue = (currentValueDisplay != null && int.TryParse(currentValueDisplay.sprite.name, out int result)) ?
			result * (currentValueNegativeSymbol != null && currentValueNegativeSymbol.gameObject.activeSelf && currentValueNegativeSymbol.color.a > 0 ? -1 : 1) : 0;
	}

	protected override void Awake() {
		base.Awake();
		
		defaultColor = currentValueDisplay?.color ?? GetComponent<SpriteRenderer>().color;
		desiredColor = defaultColor;
		color = defaultColor;
		actualValue = (currentValueDisplay != null && int.TryParse(currentValueDisplay.sprite.name, out int result)) ?
			result * (currentValueNegativeSymbol != null && currentValueNegativeSymbol.gameObject.activeSelf && currentValueNegativeSymbol.color.a > 0 ? -1 : 1) : 0;
	}

	protected override void Start() {
        base.Start();
        
        desiredColor = defaultColor;
        base9Symbols = Resources.LoadAll<Sprite>("Images/Base9/").OrderBy(s => int.Parse(s.name)).ToArray();
    }

	protected virtual void Update() {
		color = Color.Lerp(color, desiredColor, Time.deltaTime * lerpSpeed);
	}
    
#region Saving
		[Serializable]
		public class ValueDisplaySave : SerializableSaveObject<ValueDisplay> {
			public int actualValue;
			private float displayedValue;
			private float spriteAlpha;
			private SerializableColor color;
			public SerializableColor defaultColor;
			public SerializableColor desiredColor;

			public ValueDisplaySave(ValueDisplay script) : base(script) {
				this.actualValue = script.actualValue;
				this.displayedValue = script.displayedValue;
				this.spriteAlpha = script.spriteAlpha;
				this.color = script.color;
				this.defaultColor = script.defaultColor;
				this.desiredColor = script.desiredColor;
			}

			public override void LoadSave(ValueDisplay script) {
				script.actualValue = this.actualValue;
				script.displayedValue = this.displayedValue;
				script.spriteAlpha = this.spriteAlpha;
				script.color = this.color;
				script.defaultColor = this.defaultColor;
				script.desiredColor = this.desiredColor;
			}
		}
#endregion
}