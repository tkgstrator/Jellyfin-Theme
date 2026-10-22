/*
 * The only thing this script is allowed to do is write `data-item-type` back onto
 * #itemDetailPage. See docs/design.md section 1.
 *
 * Library pages expose their kind through a stable id (#moviesPage, #musicPage, ...),
 * so they need no JavaScript. The detail page is the exception: movies, albums and
 * artists all render the same #itemDetailPage, and `item.Type` never reaches the DOM.
 *
 * Everything visual lives in theme.css. If this script breaks, the detail page falls
 * back to the default palette rather than losing its layout.
 */
(() => {
	var ATTRIBUTE = "data-item-type";
	var POLL_INTERVAL_MS = 100;
	var POLL_TIMEOUT_MS = 5000;

	var types = new Map();
	var pending = null;

	// #/details?id=<guid>&serverId=<guid>
	function currentItemId() {
		var hash = window.location.hash || "";
		if (hash.indexOf("/details") === -1) {
			return null;
		}

		var query = hash.slice(hash.indexOf("?") + 1);
		return new URLSearchParams(query).get("id");
	}

	function lookupType(itemId) {
		if (types.has(itemId)) {
			return Promise.resolve(types.get(itemId));
		}

		var client = window.ApiClient;
		if (!client) {
			return Promise.resolve(null);
		}

		return client.getItem(client.getCurrentUserId(), itemId).then(
			(item) => {
				var type = item && item.Type ? item.Type : null;
				types.set(itemId, type);
				return type;
			},
			() => null,
		);
	}

	// The router swaps the page in asynchronously, so the element is usually not there
	// yet when the hash changes.
	function whenPageReady() {
		return new Promise((resolve) => {
			var deadline = Date.now() + POLL_TIMEOUT_MS;

			(function attempt() {
				var page = document.querySelector("#itemDetailPage");
				if (page || Date.now() > deadline) {
					resolve(page);
					return;
				}

				window.setTimeout(attempt, POLL_INTERVAL_MS);
			})();
		});
	}

	function apply() {
		var itemId = currentItemId();
		var token = {};
		pending = token;

		if (!itemId) {
			return;
		}

		Promise.all([whenPageReady(), lookupType(itemId)]).then((results) => {
			// A newer navigation started while we were waiting.
			if (pending !== token) {
				return;
			}

			var page = results[0];
			var type = results[1];

			if (!page) {
				return;
			}

			if (type) {
				page.setAttribute(ATTRIBUTE, type);
			} else {
				page.removeAttribute(ATTRIBUTE);
			}
		});
	}

	window.addEventListener("hashchange", apply);
	apply();
})();
