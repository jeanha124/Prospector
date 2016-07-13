using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum TurnPhase{
	idle,
	pre,
	waiting,
	post,
	gameOver
}

public class Bartok : MonoBehaviour {
	static public Bartok S;
	static public Player CURRENT_PLAYER;

	public TextAsset deckXML;
	public TextAsset layoutXML;
	public Vector3 layoutCenter = Vector3.zero;

	public float handFanDegrees = 10f;
	public int numStartingCards = 7;
	public float drawTimeStagger = 0.1f;
	public bool ___________;

	public Deck deck;
	public List<CardBartok> drawPile;
	public List<CardBartok> discardPile;

	public BartokLayout layout;
	public Transform layoutAnchor;

	public List<Player> players;
	public CardBartok targetCard;

	public TurnPhase phase = TurnPhase.idle;
	public GameObject turnLight;

	void Awake(){
		S = this;

		turnLight = GameObject.Find ("TurnLight");
	}

	void Start () {
		deck = GetComponent<Deck> (); //Get the Deck
		deck.InitDeck (deckXML.text); //Pass DeckXML to it
		Deck.Shuffle (ref deck.cards); //This shuffles the deck
		//The ref keyword passes a reference to deck.cards, which allows deck.cards to be modified by Deck.Shuffle()

		layout = GetComponent<BartokLayout> ();
		layout.ReadLayout (layoutXML.text);

		drawPile = UpgradeCardsList (deck.cards);
		LayoutGame ();
	}

	//UpgradeCardsList casts the Cards in lCD to be CardBartoks
	//Of course, they were all along, but this lets Unity know it
	List<CardBartok> UpgradeCardsList(List<Card> lCD){
		List<CardBartok> lCB = new List<CardBartok>();
		foreach (Card tCD in lCD) {
			lCB.Add (tCD as CardBartok);
		}
		return(lCB);
	}

	public void ArrangeDrawPile(){
		CardBartok tCB;

		for (int i=0; i<drawPile.Count; i++) {
			tCB = drawPile[i];
			tCB.transform.parent = layoutAnchor;
			tCB.transform.localPosition = layout.drawPile.pos;

			tCB.faceUp = false;
			tCB.SetSortingLayerName(layout.drawPile.layerName);
			tCB.SetSortOrder(-i*4);
			tCB.state = CBState.drawpile;
		}
	}

	void LayoutGame(){
		if (layoutAnchor == null) {
			GameObject tGO = new GameObject("_LayoutAnchor");
			layoutAnchor = tGO.transform;
			layoutAnchor.transform.position = layoutCenter;
		}

		ArrangeDrawPile ();

		Player pl;
		players = new List<Player> ();
		foreach (SlotDef tSD in layout.slotDefs) {
			pl = new Player();
			pl.handSlotDef = tSD;
			players.Add (pl);
			pl.playerNum = players.Count;
		}
		players [0].type = PlayerType.human;

		CardBartok tCB;

		for(int i = 0; i < numStartingCards; i++){
			for(int j=0; j < 4; j++){
				tCB = Draw();
				tCB.timeStart = Time.time + drawTimeStagger * (i*4 + j);

				players[(j+1)%4].AddCard(tCB);
			}
		}
		Invoke ("DrawFirstTarget", drawTimeStagger * (numStartingCards * 4 + 4));
	}

	public void DrawFirstTarget(){
		CardBartok tCB = MoveToTarget (Draw ());

		tCB.reportFinishTo = this.gameObject;
	}

	public void CBCallback(CardBartok cb){
		Utils.tr (Utils.RoundToPlaces (Time.time), "Bartok.CBCallback()", cb.name);

		StartGame ();
	}

	public void StartGame(){
		PassTurn (1);
	}

	public void PassTurn(int num=-1){
		if (num == -1) {
			int ndx = players.IndexOf (CURRENT_PLAYER);
			num = (ndx+1)%4;
		}
		CURRENT_PLAYER = players [num];
		phase = TurnPhase.pre;

		CURRENT_PLAYER.TakeTurn ();

		Vector3 lPos = CURRENT_PLAYER.handSlotDef.pos + Vector3.back * 5;
		turnLight.transform.position = lPos;

		Utils.tr(Utils.RoundToPlaces(Time.time), "Bartok.PassTurn()", "Old: " + lastPlayerNum, "New: " + CURRENT_PLAYER.playerNum);
	}

	public bool ValidPlay(CardBartok cb){
		if (cb.rank == targetCard.rank)
			return(true);
		if (cb.suit == targetCard.suit) {
			return(true);
		}

		return false;
	}

	public CardBartok MoveToTarget(CardBartok tCB){
		tCB.timeStart = 0;
		tCB.MoveTo (layout.discardPile.pos + Vector3.back);
		tCB.state = CBState.toTarget;
		tCB.faceUp = true;
		tCB.SetSortingLayerName ("10");
		tCB.eventualSortLayer = layout.target.layerName;
		if (targetCard != null) {
			MoveToDiscard(targetCard);
		}

		targetCard = tCB;

		return(tCB);
	}

	public CardBartok MoveToDiscard(CardBartok tCB){
		tCB.state = CBState.discard;
		discardPile.Add (tCB);
		tCB.SetSortingLayerName (layout.discardPile.layerName);
		tCB.SetSortOrder (discardPile.Count * 4);
		tCB.transform.localPosition = layout.discardPile.pos + Vector3.back / 2;

		return(tCB);
	}

	public CardBartok Draw(){
		CardBartok cd = drawPile [0];
		drawPile.RemoveAt (0);
		return(cd);
	}

	/*void Update(){
		if (Input.GetKeyDown (KeyCode.Alpha1)) {
			players [0].AddCard (Draw ());
		}
		if (Input.GetKeyDown (KeyCode.Alpha2)) {
			players [1].AddCard (Draw ());
		}
		if (Input.GetKeyDown (KeyCode.Alpha3)) {
			players [2].AddCard (Draw ());
		}
		if (Input.GetKeyDown (KeyCode.Alpha4)) {
			players [3].AddCard (Draw ());
		}
	} */
}
