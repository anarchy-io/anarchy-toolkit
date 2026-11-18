/* Adoption advisor: a wizard driven by assets/data/adoption-advisor.json.
 *
 * Each question is single or multiple choice. The next question is the first selected answer's "next" (in answer
 * order), else the question's "next", else the following question in the file; a null "next" ends the wizard.
 * Results list the suggestions of the answered questions in the order they were asked (answers in file order
 * within a question), de-duplicated by title, plus the packages they name. Answers live in the URL hash
 * (#start=existing&apps=api,job) so a result can be shared. */
(function () {
  "use strict";

  var root = document.getElementById("adoption-advisor");
  if (!root) return;

  var siteBase = new URL(root.getAttribute("data-base") || "../", window.location.href);
  var config, byId, answers = {}, drafts = {};

  function unanswer(id) {
    if (answers[id]) drafts[id] = answers[id];
    delete answers[id];
  }

  function url(path) { return new URL(path, siteBase).href; }

  function el(tag, attrs, children) {
    var node = document.createElement(tag);
    Object.keys(attrs || {}).forEach(function (key) {
      if (key === "text") node.textContent = attrs[key];
      else if (key === "onclick") node.addEventListener("click", attrs[key]);
      else node.setAttribute(key, attrs[key]);
    });
    (children || []).forEach(function (child) { if (child) node.appendChild(child); });
    return node;
  }

  function validate() {
    config.questions.forEach(function (q) {
      [q.next].concat(q.answers.map(function (a) { return a.next; })).forEach(function (next) {
        if (typeof next === "string" && !byId[next]) console.error("adoption-advisor: unknown next '" + next + "' in '" + q.id + "'");
      });
    });
  }

  function nextOf(question) {
    var selected = answers[question.id] || [];
    for (var i = 0; i < question.answers.length; i++) {
      var a = question.answers[i];
      if (selected.indexOf(a.id) >= 0 && a.next !== undefined) return a.next;
    }
    if (question.next !== undefined) return question.next;
    var index = config.questions.indexOf(question);
    return index + 1 < config.questions.length ? config.questions[index + 1].id : null;
  }

  // Rebuilds the asked path from the start, so changing an earlier answer drops answers off the new path.
  function path() {
    var ids = [], id = config.start;
    while (id && byId[id] && ids.indexOf(id) < 0) {
      ids.push(id);
      if (!(answers[id] && answers[id].length)) break;
      id = nextOf(byId[id]);
    }
    return ids;
  }

  function saveHash() {
    var parts = path().filter(function (id) { return answers[id] && answers[id].length; })
      .map(function (id) { return encodeURIComponent(id) + "=" + answers[id].map(encodeURIComponent).join(","); });
    history.replaceState(null, "", parts.length ? "#" + parts.join("&") : window.location.pathname + window.location.search);
  }

  function loadHash() {
    answers = {};
    window.location.hash.replace(/^#/, "").split("&").forEach(function (pair) {
      var kv = pair.split("=");
      if (kv.length === 2 && byId[decodeURIComponent(kv[0])]) answers[decodeURIComponent(kv[0])] = kv[1].split(",").map(decodeURIComponent).filter(Boolean);
    });
  }

  function current() {
    var ids = path(), last = ids[ids.length - 1];
    var done = answers[last] && answers[last].length && !nextOf(byId[last]);
    return done ? null : last;
  }

  function render() {
    root.innerHTML = "";
    var id = current();
    if (id) renderQuestion(byId[id]); else renderResults();
  }

  function renderQuestion(q) {
    var ids = path(), step = ids.indexOf(q.id) + 1;
    var selected = (answers[q.id] || drafts[q.id] || []).slice();
    var form = el("form", { "class": "advisor__question" });
    form.appendChild(el("p", { "class": "advisor__step", text: "Question " + step }));
    form.appendChild(el("h2", { text: q.text }));
    if (q.help) form.appendChild(el("p", { "class": "advisor__help", text: q.help }));

    q.answers.forEach(function (a) {
      var input = el("input", { type: q.multiple ? "checkbox" : "radio", name: q.id, value: a.id });
      input.checked = selected.indexOf(a.id) >= 0;
      form.appendChild(el("label", { "class": "advisor__answer" }, [input, el("span", { text: a.label })]));
    });

    var back = el("button", { type: "button", "class": "btn btn--inverse", text: "Back", onclick: function () {
      unanswer(ids[step - 2]); unanswer(q.id); saveHash(); render();
    } });
    if (step === 1) back.disabled = true;
    var next = el("button", { type: "submit", "class": "btn btn--primary", text: "Next" });
    form.appendChild(el("div", { "class": "advisor__actions" }, [back, next]));

    form.addEventListener("submit", function (event) {
      event.preventDefault();
      var picked = Array.prototype.filter.call(form.querySelectorAll("input"), function (i) { return i.checked; })
        .map(function (i) { return i.value; });
      if (!picked.length) return;
      answers[q.id] = picked;
      saveHash();
      render();
      root.scrollIntoView({ behavior: "smooth", block: "start" });
    });
    root.appendChild(form);
  }

  function renderResults() {
    var suggestions = [], titles = {}, packages = [];
    path().forEach(function (id) {
      byId[id].answers.forEach(function (a) {
        if ((answers[id] || []).indexOf(a.id) < 0) return;
        (a.suggestions || []).forEach(function (s) {
          if (titles[s.title]) return;
          titles[s.title] = true;
          suggestions.push(s);
          (s.packages || []).forEach(function (p) { if (packages.indexOf(p) < 0) packages.push(p); });
        });
      });
    });

    var result = el("div", { "class": "advisor__results" });
    result.appendChild(el("h2", { text: "Your adoption plan" }));
    result.appendChild(el("p", { text: "In this order. Each step can be its own pull request." }));
    result.appendChild(el("ol", { "class": "advisor__steps" }, suggestions.map(function (s) {
      return el("li", {}, [
        s.link ? el("a", { href: url(s.link), text: s.title }) : el("strong", { text: s.title }),
        el("p", { text: s.text })
      ]);
    })));

    if (packages.length) {
      result.appendChild(el("h3", { text: "Packages" }));
      result.appendChild(el("ul", { "class": "advisor__packages" }, packages.map(function (p) {
        return el("li", {}, [el("a", { href: url("packages/" + p + "/"), text: "Anarchy.Toolkit." + p })]);
      })));
    }

    result.appendChild(el("div", { "class": "advisor__actions" }, [
      el("button", { type: "button", "class": "btn btn--inverse", text: "Change answers", onclick: function () {
        var ids = path(); unanswer(ids[ids.length - 1]); saveHash(); render();
      } }),
      el("button", { type: "button", "class": "btn btn--primary", text: "Start over", onclick: function () {
        answers = {}; drafts = {}; saveHash(); render();
      } })
    ]));
    result.appendChild(el("p", { "class": "advisor__help", text: "Bookmark or share this page's address to come back to this plan." }));
    root.appendChild(result);
  }

  fetch(url(root.getAttribute("data-config")))
    .then(function (response) { return response.json(); })
    .then(function (json) {
      config = json;
      byId = {};
      config.questions.forEach(function (q) { byId[q.id] = q; });
      validate();
      loadHash();
      render();
      window.addEventListener("hashchange", function () { loadHash(); render(); });
    })
    .catch(function (error) {
      root.textContent = "The advisor could not load its questions. The guides cover the same ground: start with How the Platform Fits Together.";
      console.error("adoption-advisor:", error);
    });
})();
