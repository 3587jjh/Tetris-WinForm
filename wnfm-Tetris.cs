using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Remoting.Messaging;
using System.Runtime.InteropServices;

public class Tetris { // 해당 클래스는 테트리스의 논리적인 연산을 담당함
	public const int W = 10, L = 20; // 테트리스 가로, 세로
	public const int EXTRA = 20; // 테트리스 창 위로 안보이는 추가 칸
	public const int BSIZE = 4; // 배열상 블록 사이즈
	public static string[] BLOCKNAME = {"T", "O", "I", "S", "Z", "J", "L", "Garbage"};

	// Wall Kick Test Data
	private const int NUMTEST = 5;
	// I미노(시계)
	private static int[,,] KICKDATA1a = {
		{{0,0},{1,0},{-2,0},{1,2},{-2,-1}},
		{{0,0},{-2,0},{1,0},{-2,1},{1,-2}},
		{{0,0},{-1,0},{2,0},{-1,-2},{2,1}},
		{{0,0},{2,0},{-1,0},{2,-1},{-1,2}}
	};
	// I미노(반시계)
	private static int[,,] KICKDATA1b = {
		{{0,0},{-1,0},{2,0},{-1,2},{2,-1}},
		{{0,0},{-2,0},{1,0},{-2,-1},{1,2}},
		{{0,0},{1,0},{-2,0},{1,-2},{-2,1}},
		{{0,0},{2,0},{-1,0},{2,1},{-1,-2}}
	};
	// I,O미노를 제외한 다른 블록(시계)
	private static int[,,] KICKDATA2a = {
		{{0,0},{-1,0},{-1,1},{0,-2},{-1,-2}},
		{{0,0},{-1,0},{-1,-1},{0,2},{-1,2}},
		{{0,0},{1,0},{1,1},{0,-2},{1,-2}},
		{{0,0},{1,0},{1,-1},{0,2},{1,2}}
	};
	// I,O미노를 제외한 다른 블록(반시계)
	private static int[,,] KICKDATA2b = {
		{{0,0},{1,0},{1,1},{0,-2},{1,-2}},
		{{0,0},{-1,0},{-1,-1},{0,2},{-1,2}},
		{{0,0},{-1,0},{-1,1},{0,-2},{-1,-2}},
		{{0,0},{1,0},{1,-1},{0,2},{1,2}}
	};
	public Tetris() {} // 의도적으로 비움
	public class Block {
		public int x, y; // 필드 배열상에서의 위치
		public int type, curRot;
		public int[,,] state = new int[4, BSIZE, BSIZE]; // 회전상태, 블록x, 블록y
		public bool isShadow;
		public Block() {} // 의도적으로 비움
		public Block(int _type) {
			curRot = 0;
			type = _type;
			x = 3;
			y = EXTRA-1;
			isShadow = false;

			if (type==0) { // T미노
				state[0, 0, 1]=state[0, 1, 0]=state[0, 1, 1]=state[0, 2, 1]=1;
				state[1, 1, 0]=state[1, 1, 1]=state[1, 1, 2]=state[1, 2, 1]=1;
				state[2, 0, 1]=state[2, 1, 1]=state[2, 1, 2]=state[2, 2, 1]=1;
				state[3, 0, 1]=state[3, 1, 0]=state[3, 1, 1]=state[3, 1, 2]=1;
			}
			else if (type==1) { // O미노
				for (int i = 0; i<4; ++i) {
					state[i, 1, 0]=state[i, 2, 0]=state[i, 1, 1]=state[i, 2, 1]=1;
				}
			}
			else if (type==2) { // I미노
				state[0, 0, 1]=state[0, 1, 1]=state[0, 2, 1]=state[0, 3, 1]=1;
				state[1, 2, 0]=state[1, 2, 1]=state[1, 2, 2]=state[1, 2, 3]=1;
				state[2, 0, 2]=state[2, 1, 2]=state[2, 2, 2]=state[2, 3, 2]=1;
				state[3, 1, 0]=state[3, 1, 1]=state[3, 1, 2]=state[3, 1, 3]=1;
			}
			else if (type==3) { // S미노
				state[0, 0, 1]=state[0, 1, 0]=state[0, 1, 1]=state[0, 2, 0]=1;
				state[1, 1, 0]=state[1, 1, 1]=state[1, 2, 1]=state[1, 2, 2]=1;
				state[2, 0, 2]=state[2, 1, 1]=state[2, 1, 2]=state[2, 2, 1]=1;
				state[3, 0, 0]=state[3, 0, 1]=state[3, 1, 1]=state[3, 1, 2]=1;
			}
			else if (type==4) { // Z미노
				state[0, 0, 0]=state[0, 1, 0]=state[0, 1, 1]=state[0, 2, 1]=1;
				state[1, 1, 1]=state[1, 1, 2]=state[1, 2, 0]=state[1, 2, 1]=1;
				state[2, 0, 1]=state[2, 1, 1]=state[2, 1, 2]=state[2, 2, 2]=1;
				state[3, 0, 1]=state[3, 0, 2]=state[3, 1, 0]=state[3, 1, 1]=1;
			}
			else if (type==5) { // J미노
				state[0, 0, 0]=state[0, 0, 1]=state[0, 1, 1]=state[0, 2, 1]=1;
				state[1, 1, 0]=state[1, 1, 1]=state[1, 1, 2]=state[1, 2, 0]=1;
				state[2, 0, 1]=state[2, 1, 1]=state[2, 2, 1]=state[2, 2, 2]=1;
				state[3, 0, 2]=state[3, 1, 0]=state[3, 1, 1]=state[3, 1, 2]=1;
			}
			else { // L미노
				state[0, 0, 1]=state[0, 1, 1]=state[0, 2, 0]=state[0, 2, 1]=1;
				state[1, 1, 0]=state[1, 1, 1]=state[1, 1, 2]=state[1, 2, 2]=1;
				state[2, 0, 1]=state[2, 0, 2]=state[2, 1, 1]=state[2, 2, 1]=1;
				state[3, 0, 0]=state[3, 1, 0]=state[3, 1, 1]=state[3, 1, 2]=1;
			}
		}
	}
	public class BlockMaker {
		public const int NUMTYPE = 7; // 블록 종류 수
		public const int NUMCAND = 5; // Next블록 수
		public int[] nextCand = new int[NUMCAND]; // 화면상에 보이는 next블록들
		private int[] group = new int[NUMTYPE]; // 현재 블록 가방
		private int gIdx;
		public BlockMaker() {
			for (int i = 0; i < NUMTYPE; ++i) group[i] = i;
			Shuffle(ref group);
			for (int i = 0; i < NUMCAND; ++i) nextCand[i] = group[i];
			gIdx = NUMCAND;
		}
		public Block Next() {
			Block ret = new Block(nextCand[0]);
			for (int i = 0; i < NUMCAND; ++i) {
				if (i+1 == NUMCAND) {
					nextCand[i] = group[gIdx++];
					if (gIdx == NUMTYPE) {
						gIdx = 0;
						Shuffle(ref group);
					}
				}
				else nextCand[i] = nextCand[i+1];
			}
			return ret;
		}
	}
	public class Field {
		// (x,y)에는 state[x,y]타입의 블록이 들어있음
		public int[,] state = new int[W, L+EXTRA];
		public const int NONE = -1;
		public const int GARBAGE = 7;
		public const int DROPTIME = 1000; // 블록이 떨어지는 시간 간격
		public const int LOCKDELAYTIME = 500;
		//## 주기적으로 랜덤 몇 줄씩 올라온다는 정보 필요 (현재는 1칸으로 설정)
		public int gHole, gTime;
		public Field() {
			for (int i = 0; i < W; ++i)
				for (int j = 0; j < L+EXTRA; ++j)
					state[i,j] = NONE; // 어떤 블록도 있지 않음
			Random r = new Random();
			//##
			gHole = r.Next(0, W);
			gTime = 10000;
		}
		public bool CanPut(Block b) {
			for (int i = 0; i < BSIZE; ++i)
				for (int j = 0; j < BSIZE; ++j) {
					if (b.state[b.curRot,i,j] == 0) continue;
					int cx = b.x+i, cy = b.y+j;
					if (cx<0 || cx>=W || cy<0 || cy>=L+EXTRA || state[cx, cy]!=NONE) {
						return false;
					}
				}
			return true;
		}
		public void Put(Block b, bool remove) { // b는 필드에 놓기 적합하다고 가정
			for (int i = 0; i<BSIZE; ++i)
				for (int j = 0; j<BSIZE; ++j)
					if (b.state[b.curRot,i,j]==1 && !b.isShadow) {
						int cx = b.x+i, cy = b.y+j;
						if (remove) state[cx,cy] = NONE;
						else state[cx, cy] = b.type;
					}
		}
		public int RemoveLine() { // 없앤 줄 수 반환
			int x, y, uy, cnt;
			int ret = 0;
			for (y=L+EXTRA-1; y>=0; --y) {
				cnt=0;
				for (x=0; x<W; ++x) {
					if (state[x, y] != NONE) ++cnt;
				}
				if (cnt==W) break;
			}
			uy=y;
			if (y<0) return 0;
			while (uy>=0) {
				cnt=0;
				for (x=0; x<W; ++x) {
					if (state[x, uy] != NONE) ++cnt;
				}
				if (cnt<W) {
					for (x=0; x<W; ++x) {
						state[x, y]=state[x, uy];
					}
					--y;
				}
				else ++ret;
				--uy;
			}
			for (uy=0; uy<=y; ++uy)
				for (x=0; x<W; ++x)
					state[x, uy]=NONE;
			return ret;
		}
		public void UpdateVal(int rLine) {
			//## 콤보, T스핀횟수, 공격량 등 갱신
			//## 매개변수를 좀 더 추가해야함
		}
		public Block GetShadow(Block b) {
			// b는 잠시 state상에서 없앤 상태임을 가정
			Block ret = new Block(b.type);
			ret.x = b.x; ret.y = b.y; ret.curRot = b.curRot;
			ret.isShadow = true;
			while (CanPut(ret)) ++ret.y;
			if (ret.y != b.y) --ret.y;
			return ret;
		}
		private void KickWall(ref Block b, bool clockwise) {
			if (b.type==1) return; // O
			for (int t = 0; t<NUMTEST; ++t) {
				Block nb = new Block(b.type);
				nb.curRot=b.curRot;

				if (b.type==2) { // I
					if (clockwise) {
						nb.x=b.x+KICKDATA1a[b.curRot, t, 0];
						nb.y=b.y+KICKDATA1a[b.curRot, t, 1];
					}
					else {
						nb.x=b.x+KICKDATA1b[b.curRot, t, 0];
						nb.y=b.y+KICKDATA1b[b.curRot, t, 1];
					}
				}
				else {
					if (clockwise) {
						nb.x=b.x+KICKDATA2a[b.curRot, t, 0];
						nb.y=b.y+KICKDATA2a[b.curRot, t, 1];
					}
					else {
						nb.x=b.x+KICKDATA2b[b.curRot, t, 0];
						nb.y=b.y+KICKDATA2b[b.curRot, t, 1];
					}
				}
				if (CanPut(nb)) { b=nb; return; }
			}
		}
		public Block BlockMove(Block b, string s) {
			// b는 필드에 들어가기 적합하다고 가정
			// 임시로 필드 state상에서 b의 존재를 지우기 (화면에서는 그대로)
			for (int i = 0; i<BSIZE; ++i)
				for (int j = 0; j<BSIZE; ++j)
					if (b.state[b.curRot, i, j] == 1) {
						int cx = b.x+i, cy = b.y+j;
						state[cx, cy] = NONE;
					}
			Block nb = new Block(b.type);
			nb.x=b.x; nb.y=b.y; nb.curRot=b.curRot;

			if (s=="Left") --nb.x;
			else if (s=="Right") ++nb.x;
			else if (s=="Down") ++nb.y;
			else if (s=="Space") nb.y=GetShadow(b).y;
			else if (s=="Up"||s=="X") { // 시계방향 회전
				nb.curRot=(nb.curRot+1)%4;
				KickWall(ref nb, true);
			}
			else { // s == "Z", 반시계방향 회전
				nb.curRot=(nb.curRot+3)%4;
				KickWall(ref nb, false);
			}
			Block ret = nb;
			if (!CanPut(nb)) ret = b;
			// state상에서 b를 다시 나타내기
			for (int i = 0; i<BSIZE; ++i)
				for (int j = 0; j<BSIZE; ++j)
					if (b.state[b.curRot, i, j]==1) {
						int cx = b.x+i, cy = b.y+j;
						state[cx, cy]=b.type;
					}
			return ret;
		}
		public void CreateGarbage() {
			int i, j;
			//##한 칸씩 위로 올리기
			for (i = 0; i < W; ++i)
				for (j = 1; j < L+EXTRA; ++j)
					state[i,j-1] = state[i,j];
			// 맨 아래에 쓰레기줄 생성
			for (i = 0; i < W; ++i) {
				state[i,L+EXTRA-1] = GARBAGE;
			}
			state[gHole, L+EXTRA-1] = NONE;
		}
	}
	private static void Shuffle(ref int[] A) {
		Random r = new Random();
		for (int iter = 0; iter < 10; ++iter)
			for (int i = A.Length-1; i > 0; --i) {
				int j = r.Next(0, i+1);
				int tmp = A[i]; A[i] = A[j]; A[j] = tmp;
			}
	}
}
public class MyBox {
	public int stx, sty, w, h;
	public Bitmap baseBmp;
	public bool added;
	public MyBox() {} // 의도적으로 비움
	public MyBox(int _stx, int _sty, int _w, int _h) {
		stx = _stx; sty = _sty; w = _w; h = _h;
		added = false;
		baseBmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
		using (Graphics gr = Graphics.FromImage(baseBmp)) {
			gr.CopyFromScreen(TetrisForm.FORMX+stx, TetrisForm.FORMY+sty,
				0, 0, baseBmp.Size);
		}
	}
}
public class MyPanel : Panel {
	protected override void OnPaintBackground(PaintEventArgs e) {
		// 의도적으로 비움
	}
}
public class TetrisForm : Form {
	Button startButton;
	MyPanel basePanel;
	private const int BLOCKSIZE = 25;
	private const int STX = 350, STY = 150; // Grid 시작 위치
	public static int FORMX, FORMY; // 화면 시작 위치
	private Bitmap[] bBlock = new Bitmap[Tetris.BlockMaker.NUMTYPE+1];
	private Bitmap[] bCut = new Bitmap[Tetris.BlockMaker.NUMTYPE+1];
	private Bitmap[] bComplete = new Bitmap[Tetris.BlockMaker.NUMTYPE];
	private Bitmap[] bShadow = new Bitmap[Tetris.BlockMaker.NUMTYPE];
	private Bitmap bBackground, bGrid, bSkin;
	private Bitmap[] bNextBox = new Bitmap[2];
	private MyBox[,] mField = new MyBox[Tetris.W, Tetris.L+1];
	private MyBox[] mNextBox = new MyBox[Tetris.BlockMaker.NUMCAND];
	private MyBox mHoldBox;
	public TetrisForm() {InitializeComponent();}
	public void InitializeComponent() {
		//## 드래그 금지 설정할 것
		this.Size = new Size(1024, 768);
		this.Text = "PuyoTetris Simulator";
		this.BackColor = Color.Black;
		this.FormBorderStyle = FormBorderStyle.FixedSingle;
		this.StartPosition = FormStartPosition.CenterScreen;
		this.MaximizeBox = this.MinimizeBox = false;

		startButton = new Button();
		startButton.BackColor = Color.White;
		startButton.ForeColor = Color.Blue;
		startButton.Text = "시작";
		startButton.Click += new EventHandler(OnStartButtonClick);
		startButton.Size = new Size(200, 80);
		startButton.Location = new Point(400, 300);
		Controls.Add(startButton);
	}
	private void OnStartButtonClick(object sender, EventArgs e) {
		startButton.Dispose();
		FORMX = this.Location.X+3;
		FORMY = this.Location.Y+25;
		SetStyle(ControlStyles.UserPaint, true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, true);

		LoadBitmap();
		LoadInterface();
		LoadMyBox();
		StartGame();
	}
	private void LoadBitmap() { // 모든 이미지 파일을 미리 로드해둔다
		for (int i = 0; i <= Tetris.BlockMaker.NUMTYPE; ++i) {
			bBlock[i] = new Bitmap("./graphic/Block_" + Tetris.BLOCKNAME[i] + ".png");
			bBlock[i] = ConvertFormat(bBlock[i]);
			bCut[i] = new Bitmap("./graphic/Block_" +Tetris.BLOCKNAME[i] + "_Cut.png");
			bCut[i] = ConvertFormat(bCut[i]);
			if (i < Tetris.BlockMaker.NUMTYPE) {
				bComplete[i] = new Bitmap("./graphic/Block_"+Tetris.BLOCKNAME[i]+"_Complete.png");
				bComplete[i] = ConvertFormat(bComplete[i]);
				bShadow[i] = new Bitmap("./graphic/Block_"+Tetris.BLOCKNAME[i]+"_Shadow.png");
				bShadow[i] = ConvertFormat(bShadow[i]);
			}
		}
		bBackground = new Bitmap("./graphic/Background.png");
		bBackground = ConvertFormat(bBackground);
		bGrid = new Bitmap("./graphic/Grid_1.png");
		bGrid = ConvertFormat(bGrid);
		bSkin = new Bitmap("./graphic/Skin_Pink.png");
		bSkin = ConvertFormat(bSkin);
		bNextBox[0] = new Bitmap("./graphic/Next_1.png");
		bNextBox[0] = ConvertFormat(bNextBox[0]);
		bNextBox[1] =new Bitmap("./graphic/Next_2.png");
		bNextBox[1] = ConvertFormat(bNextBox[1]);
	}
	private void LoadInterface() {
		basePanel = new MyPanel();
		basePanel.Paint += new PaintEventHandler(basePanel_Paint);
		basePanel.BackColor = Color.Black;
		basePanel.Location = new Point(0, 0);
		basePanel.Size = bBackground.Size;
		Controls.Add(basePanel);

		MergeImage(ref bBackground, bGrid, STX, STY);
		MergeImage(ref bBackground, bSkin, STX-112, STY-31);
		MergeImage(ref bBackground, bNextBox[0], STX+270, STY);
		for (int i = 0; i+1 < Tetris.BlockMaker.NUMCAND; ++i) {
			MergeImage(ref bBackground, bNextBox[1], STX+270, STY+105+72*i);
		}
		AddImage(bBackground, 0, 0);
	}
	private void LoadMyBox() {
		for (int i = 0; i < Tetris.W; ++i)
			for (int j = 0; j < Tetris.L+1; ++j) {
				if (j == 0) mField[i,j] = new MyBox(STX+BLOCKSIZE*i, STY+BLOCKSIZE-8, BLOCKSIZE, 8);
				else mField[i, j]=new MyBox(STX+BLOCKSIZE*i, STY+BLOCKSIZE*j, BLOCKSIZE, BLOCKSIZE);
			}
		for (int i = 0; i < Tetris.BlockMaker.NUMCAND; ++i) 
			mNextBox[i] = new MyBox(STX+270, STY+33+72*i, bNextBox[1].Width, bNextBox[1].Height);
		mHoldBox = new MyBox(STX-104, STY+30, bNextBox[1].Width, bNextBox[1].Height);
	}

	/*************************************************************************************************/
	Tetris.Field field;
	private const int MAX = 30;
	private Timer blockTimer;
	private Tetris.Block cur;
	private Tetris.BlockMaker rng;
	private bool wantNext; // 다음 블록을 가져올 것인가
	private bool holdUsed; // 게임에서 홀드가 한 번이라도 사용됐는가 (홀드창 블럭 유무)
	private bool canHold; // 현재 낙하 사이클에 홀드를 쓸 수 있는가 (연속 금지)
	private bool curHeld; // 현재 블록에 대해 홀드를 작동시켰는가
	private Tetris.Block hBlock; // 홀드된 블럭
	private Tetris.Block tmp; // cur과 hBlock을 swap하기 위한 임시 저장 매체
	private bool isLockDelay;
	private bool recentMove;
	private string recentS;

	private void StartGame() {
		field = new Tetris.Field();
		cur = new Tetris.Block();
		rng = new Tetris.BlockMaker();
		wantNext = true;
		holdUsed = false;
		canHold = true;
		curHeld = false;
		hBlock = new Tetris.Block();
		tmp = new Tetris.Block();

		InitNextBlock();
		UpdateInformation();
		//## 스타트 카운트 프로세스 실행
		// 게임 스타트
		blockTimer = new Timer();
		blockTimer.Tick += new EventHandler(OnBlockTimerTick);
		Process_NextBlock();
		//## 게임 오버 프로세스 실행
	}
	private void Process_NextBlock() {
		if (wantNext) cur = GetNextBlock(ref rng);
		else cur = new Tetris.Block(tmp.type);
		bool gameOver = false;
		if (!field.CanPut(cur)) --cur.y;
		if (!field.CanPut(cur)) gameOver = true;
		if (!gameOver) Put(field.GetShadow(cur), false);
		Put(cur, false);
		if (gameOver) return;

		// 현재 블록 내리기
		isLockDelay = LockDelay(cur);
		recentMove = false;
		recentS = "";
		
		if (isLockDelay) blockTimer.Interval = Tetris.Field.LOCKDELAYTIME;
		else blockTimer.Interval = Tetris.Field.DROPTIME;
		this.KeyDown += new KeyEventHandler(TetrisForm_KeyDown);
		blockTimer.Start();
	}
	private void OnBlockTimerTick(object sender, EventArgs e) {
		blockTimer.Stop();
		this.KeyDown -= new KeyEventHandler(TetrisForm_KeyDown);

		if (!curHeld) { // lockTimer 또는 dropTimer 다 됨
			if (recentS != "Space" && isLockDelay && recentMove) {
				// Lock Delay 시간 초기화
				isLockDelay = LockDelay(cur);
				recentMove = false;
				recentS = "";
				blockTimer.Interval = Tetris.Field.LOCKDELAYTIME;
				blockTimer.Start();
				this.KeyDown+=new KeyEventHandler(TetrisForm_KeyDown);
				return;
			}
			int prev = cur.y;
			BlockMove(ref cur, "Down");
			if (cur.y == prev) {
				wantNext = true;
				canHold = true;
				UpdateInformation();
				//## CreateGarbage();
				// 쓰레기 줄은 진행되는 동안 계속 누적되고, 이때 한 번에 생겨남
				Process_NextBlock();
			}
			else {
				isLockDelay = LockDelay(cur);
				recentMove = false;
				recentS = "";
				blockTimer.Interval = Tetris.Field.DROPTIME;
				blockTimer.Start();
				this.KeyDown+=new KeyEventHandler(TetrisForm_KeyDown);
				return;
			}
		}
		else { // 기존에 있던 cur을 지우기
			Put(cur, true);
			Put(field.GetShadow(cur), true);
			curHeld = false;
			Process_NextBlock();
		}
	}
	private void RestartTimer(Timer myTimer, int interval) {
		myTimer.Stop();
		myTimer.Interval = interval;
		myTimer.Start();
	}
	private void TetrisForm_KeyDown(object sender, KeyEventArgs e) {
		string s = e.KeyCode.ToString();
		if (s == "P") {
			//## 일시 정지
		}
		else if (s == "C") { // 홀드
			if (!canHold) return;
			canHold = false;
			curHeld = true;

			Put(hBlock, true);
			tmp = new Tetris.Block(hBlock.type);
			hBlock = new Tetris.Block(cur.type);
			DrawHoldBlock(cur.type);

			if (!holdUsed) {
				holdUsed = true;
				wantNext = true;
			}
			else wantNext = false;
			RestartTimer(blockTimer, 1);
		}
		else if (s=="Left" || s=="Right" || s=="Up" || s=="Down" ||
			s=="Space" || s=="Z" || s=="X") {
			int prev = cur.y;
			recentMove = BlockMove(ref cur, s);
			recentS = s;

			if (s == "Space") RestartTimer(blockTimer, 1);
			else if (recentMove) {
				if (isLockDelay) {
					if (!LockDelay(cur)) {
						isLockDelay = false;
						this.KeyDown -= new KeyEventHandler(TetrisForm_KeyDown);
						RestartTimer(blockTimer, Tetris.Field.DROPTIME);
						this.KeyDown += new KeyEventHandler(TetrisForm_KeyDown);
					}
					else RestartTimer(blockTimer, 1);
				}
				else if (LockDelay(cur)) {
					isLockDelay = true;
					this.KeyDown-=new KeyEventHandler(TetrisForm_KeyDown);
					RestartTimer(blockTimer, Tetris.Field.LOCKDELAYTIME);
					this.KeyDown+=new KeyEventHandler(TetrisForm_KeyDown);
				}
			}
		}
	}

	/*************************************************************************************************/
	private void DrawNextBlock(int bType, int n) {
		RemoveImage(ref mNextBox[n]);
		AddImage(ref mNextBox[n], bComplete[bType]);
	}
	private void InitNextBlock() {
		for (int i = 0; i < Tetris.BlockMaker.NUMCAND; ++i) {
			DrawNextBlock(rng.nextCand[i], i);
		}
	}
	private Tetris.Block GetNextBlock(ref Tetris.BlockMaker rng) {
		Tetris.Block ret = rng.Next();
		for (int i = 0; i < Tetris.BlockMaker.NUMCAND; ++i) {
			DrawNextBlock(rng.nextCand[i], i);
		}
		return ret;
	}
	private void DrawField() {
		for (int i = 0; i < Tetris.W; ++i) { 
			for (int j = Tetris.EXTRA-1; j < Tetris.EXTRA+Tetris.L; ++j) {
				if (field.state[i,j] == Tetris.Field.NONE) {
					RemoveImage(ref mField[i, j-Tetris.EXTRA+1]);
				}
				else {
					int type = field.state[i,j];
					int cy = j-Tetris.EXTRA+1;
					if (cy == 0) AddImage(ref mField[i,cy], bCut[type]);
					else AddImage(ref mField[i,cy], bBlock[type]);
				}
			}
		}
	}
	private void UpdateVal(int rLine) {
		//## 모든 게임 정보값을 재출력
	}
	private void UpdateInformation() {
		int rLine = field.RemoveLine();
		field.UpdateVal(rLine);
		DrawField();
		UpdateVal(rLine);
	}
	private void Put(Tetris.Block b, bool remove) {
		field.Put(b, remove);
		Bitmap bmp;
		if (!b.isShadow) bmp = bBlock[b.type];
		else bmp = bShadow[b.type];
		Bitmap bmp2 = bCut[b.type];

		for (int i = 0; i < Tetris.BSIZE; ++i) { 
			for (int j = 0; j < Tetris.BSIZE; ++j) {
				int cx = b.x+i, cy = b.y+j-Tetris.EXTRA+1;
				if (b.state[b.curRot,i,j] == 1 && cy >= 0) {
					if (remove) RemoveImage(ref mField[cx, cy]);
					else {
						if (cy > 0) AddImage(ref mField[cx,cy], bmp);
						else if (!b.isShadow) AddImage(ref mField[cx,cy], bmp2);
					}
				}
			}
		}
	}
	private bool LockDelay(Tetris.Block b) {
		// b가 더 내려갈 곳이 있는지 판단
		// 임시로 필드 state상에서 b의 존재를 지우기 (화면에서는 그대로)
		// b는 필드에 들어가기 적합하다고 가정
		for (int i = 0; i<Tetris.BSIZE; ++i)
			for (int j = 0; j<Tetris.BSIZE; ++j) {
				if (b.state[b.curRot, i, j]==1) { 
					int cx = b.x+i, cy = b.y+j;
					field.state[cx,cy] = Tetris.Field.NONE;
				}
			}
		++b.y;
		bool ret = !field.CanPut(b);
		--b.y;
		// state상에서 b를 다시 나타내기
		for (int i = 0; i<Tetris.BSIZE; ++i)
			for (int j = 0; j<Tetris.BSIZE; ++j) {
				if (b.state[b.curRot, i, j]==1) {
					int cx = b.x+i, cy = b.y+j;
					field.state[cx, cy]=b.type;
				}
			}
		return ret;
	}
	private void DrawHoldBlock(int bType) {
		RemoveImage(ref mHoldBox);
		AddImage(ref mHoldBox, bComplete[bType]);
	}
	private bool BlockMove(ref Tetris.Block cur, string s) {
		Tetris.Block afterMove = field.BlockMove(cur, s);
		bool moved = (afterMove.x != cur.x) || (afterMove.y != cur.y) ||
			(afterMove.curRot != cur.curRot);
		if (moved) {
			Put(cur, true);
			if (s != "Down" && s != "Space") Put(field.GetShadow(cur), true);
			cur = afterMove;
			if (s!="Down"&&s!="Space") Put(field.GetShadow(cur), false);
			Put(cur, false);
		}
		return moved;
	}
	private void CreateGarbage() {
		field.CreateGarbage();
		DrawField();
	}

	/*************************************************************************************************/
	private Bitmap sbmp;
	private int sx, sy;

	private void basePanel_Paint(object sender, PaintEventArgs e) {
		/*
		//## Reflection 실패시 사용
		using (BufferedGraphicsContext context = new BufferedGraphicsContext())
		using (BufferedGraphics buffer = context.Allocate(e.Graphics,
			new Rectangle(sx, sy, sbmp.Width, sbmp.Height))) {
			buffer.Graphics.DrawImageUnscaled(sbmp, sx, sy);
			buffer.Render();
		} */
		e.Graphics.DrawImageUnscaled(sbmp, sx, sy);
	}
	private void AddImage(Bitmap bmp, int x, int y) {
		sbmp = bmp; sx = x; sy = y;
		basePanel.Refresh();
	}
	private void AddImage(ref MyBox mb, Bitmap bmp) {
		sbmp = bmp; sx = mb.stx; sy = mb.sty;
		basePanel.Refresh();
		mb.added = true;
	}
	private void RemoveImage(ref MyBox mb) {
		if (!mb.added) return;
		sbmp = mb.baseBmp; sx = mb.stx; sy = mb.sty;
		basePanel.Refresh();
		mb.added=false;
	}
	private void MergeImage(ref Bitmap big, Bitmap small, int x, int y) {
		Graphics g = Graphics.FromImage(big);
		g.DrawImage(small, new Point(x, y));
	}
	private Bitmap SetImageOpacity(Bitmap bmp, double opacity) {
		Bitmap ret = new Bitmap(bmp.Width, bmp.Height);
		Graphics gr = Graphics.FromImage(ret);
		ColorMatrix matrix = new ColorMatrix();
		matrix.Matrix33 = (float)opacity;
		ImageAttributes attribute = new ImageAttributes();
		attribute.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
		gr.DrawImage(bmp, new Rectangle(0, 0, ret.Width, ret.Height), 0, 0, bmp.Width,
			bmp.Height, GraphicsUnit.Pixel, attribute);
		return ret;
	}
	private Bitmap ConvertFormat(Bitmap bmp) {
		Bitmap ret = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppPArgb);
		using (Graphics gr = Graphics.FromImage(ret)) {
			gr.DrawImage(bmp, 0, 0);
		}
		return ret;
	}
}
public class WnfmMain {
	public static int Main() {
		Application.Run(new TetrisForm());
		return 0;
	}
}