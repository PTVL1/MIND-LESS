const cards = document.querySelectorAll(".section-card");
const currentMonth = new Date().getMonth() + 1;
const intro = document.getElementById("intro");
const detailTitle = document.getElementById("detailTitle");
const detailDescription = document.getElementById("detailDescription");
const detailStatus = document.getElementById("detailStatus");
const timelineStatus = document.getElementById("timelineStatus");
const timeline = document.querySelector(".timeline");
const bookOverlay = document.getElementById("bookOverlay");
const bookClose = document.getElementById("bookClose");
const bookNumber = document.getElementById("bookNumber");
const bookTitle = document.getElementById("bookTitle");
const bookSubtitle = document.getElementById("bookSubtitle");
const bookDescription = document.getElementById("bookDescription");
const bookDetails = document.getElementById("bookDetails");
const monthNames = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
];

document.body.classList.add("reveal-ready");
timelineStatus.textContent = `Current month: ${monthNames[currentMonth - 1]}`;

const updateDetail = (card) => {
  const month = Number(card.dataset.month);
  const title = card.querySelector(".section-title").textContent;
  const description = card.dataset.description;
  const locked = month > currentMonth;

  detailTitle.textContent = title;
  detailDescription.textContent = description;
  detailStatus.textContent = locked
    ? `Locked · unlocks in ${monthNames[month - 1]}`
    : `Available · ${monthNames[month - 1]}`;
  detailStatus.dataset.state = locked ? "locked" : "open";
};

const openBook = (card) => {
  const month = Number(card.dataset.month);
  bookNumber.textContent = `Q${month}`;
  bookTitle.textContent = card.querySelector(".section-title").textContent;
  bookSubtitle.textContent = "achieving imperfection";
  bookDescription.textContent = card.dataset.description;
  bookDetails.textContent = card.dataset.details;
  bookOverlay.classList.add("active");
  bookOverlay.setAttribute("aria-hidden", "false");
  document.body.style.overflow = "hidden";
};

const closeBook = () => {
  bookOverlay.classList.remove("active");
  bookOverlay.setAttribute("aria-hidden", "true");
  document.body.style.overflow = "";
};

const revealCards = () => {
  if (!timeline) {
    return;
  }
  const rect = timeline.getBoundingClientRect();
  const threshold = window.innerHeight;
  if (rect.top < threshold * 0.75) {
    document.querySelectorAll(".reveal-card:not(.reveal-late)").forEach((card) => {
      card.classList.add("visible");
    });
  }
  if (rect.top < threshold * 0.35) {
    document.querySelectorAll(".reveal-card.reveal-late").forEach((card) => {
      card.classList.add("visible");
    });
  }
};

cards.forEach((card, index) => {
  const month = Number(card.dataset.month);
  const delay = index * 0.08;
  card.style.animationDelay = `${delay}s`;

  if (month > currentMonth) {
    card.classList.add("locked");
  }

  card.addEventListener("click", () => {
    cards.forEach((item) => item.classList.remove("active"));
    card.classList.add("active");
    updateDetail(card);
    if (month <= currentMonth) {
      openBook(card);
    }
  });
});

const firstAvailable = Array.from(cards).find(
  (card) => Number(card.dataset.month) <= currentMonth
);
if (firstAvailable) {
  firstAvailable.classList.add("active");
  updateDetail(firstAvailable);
}

revealCards();
window.addEventListener("scroll", revealCards);
window.addEventListener("resize", revealCards);

bookClose.addEventListener("click", closeBook);
bookOverlay.addEventListener("click", (event) => {
  if (event.target === bookOverlay) {
    closeBook();
  }
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape" && bookOverlay.classList.contains("active")) {
    closeBook();
  }
});

if (intro) {
  setTimeout(() => {
    document.body.classList.add("intro-complete");
    setTimeout(() => {
      intro.remove();
    }, 400);
  }, 2400);
}
